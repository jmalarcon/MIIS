using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Web;
using System.Web.Caching;
using IISHelpers;
using DotLiquid;
using MIISHandler.Filters;
using MIISHandler.FMSources;

namespace MIISHandler
{
    /// <summary>
    /// Renders the final HTML from Markdown using the spcified template or CSS file
    /// </summary>
    public static class HTMLRenderer
    {
        #region Constants
        //This is simply a plain HTML5 file to show the contents inside if there's no template specified, 
        //to ensure at least a valid HTML5 page returned and not just a bunch of HTML tags
        private const string DEFAULT_TEMPLATE =
@"<!doctype html>
<html>
<head>
    <title>{{title}}</title>
    <link rel=""stylesheet"" href=""{{cssfile}}"">
</head>
<body>
{{content}}
</body>
</html>";
        private static string FILE_INCLUDES_PREFIX = "$";  //How to identify includes placeholders in layout files
        private static string FILE_FRAGMENT_PREFIX = "*";  //How to identify fragments placeholders in content files
        #endregion

        #region Main Method - Render
        /// <summary>
        /// Renders the HTML from the markdown using the templates and parameters specified in web.config
        /// and processing the templates
        /// </summary>
        /// <param name="md">The markdown file information</param>
        /// <returns>The final HTML to return to the client</returns>
        public static string RenderMarkdown(MarkdownFile md)
        {
            HttpContext ctx = HttpContext.Current;
            string template = DEFAULT_TEMPLATE; //The default template for the final HTML
            string templateFile = GetCurrentTemplateFile(md);
            if (!string.IsNullOrEmpty(templateFile))
            {
                //If the specified template is "raw" then just return the raw HTML without any wrapping HTML code 
                //(no html, head or body tags). Useful to return special pages with raw content.
                if (templateFile == "raw")
                {
                    template = "{{content}}";
                }
                else
                {
                    List<string> templateDependencies = new List<string>();
                    template = ReadTemplate(templateFile, ctx, templateDependencies);    //Read, transform and cache template
                    //Add template file's dependences as dependences for the Markdown file cache too
                    md.Dependencies.AddRange(templateDependencies);
                }
            }

            //First process the "content" field with the main HTML content transformed from Markdown
            //This allows to use other fields inside the content itself, not only in the templates
            string finalContent = TemplatingHelper.ReplacePlaceHolder(template, "content", md.RawHTML);

            //Process fragments (other files inserted into the current one or template)
            finalContent = ProcessFragments(finalContent, md, ctx);

            //Dynamically setup and add to DotLiquid template processor all the new custom tags, TODO: and filters
            RegisterCustomExtensions();   //Register custom MIIS Liquid Tags, Filters and Front-Matter sources

            //Process template + content + fields with DotLiquid
            Template parser = Template.Parse(finalContent);
            Hash fieldsInfo = new MDFieldsResolver(md, ctx);
            finalContent = parser.Render(fieldsInfo);

            //Transform virtual paths
            finalContent = WebHelper.TransformVirtualPaths(finalContent);

            //Return the transformed file
            return finalContent;
        }
        #endregion

        #region Aux methods
        /// <summary>
        /// Gets the relative path of the template to use with the current file taking into account all the possible parameters/fields that control this setting
        /// </summary>
        /// <returns></returns>
        private static string GetCurrentTemplateFile(MarkdownFile md)
        {
            //Get the template name that is going to be used (Front Matter or configuration), if any.
            string templateName = FieldValuesHelper.GetFieldValue("TemplateName", md);
            if (string.IsNullOrEmpty(templateName) || templateName.ToLower() == "none")
                return string.Empty;    //Use the default basic HTML5 template

            if (templateName.ToLower() == "raw")
                return "raw";   //Use raw contents, without any wrapping HTML tags

            //The name (or sub-path) for the layout file (.html normaly) to be used
            string layoutName = FieldValuesHelper.GetFieldValue("Layout", md);
            if (string.IsNullOrEmpty(layoutName))
                return string.Empty;    //Use the default basic HTML5 template

            //If both the template folder and the layout are established, then get the base folder for the templates
            //This base path for the templates parameter is only available through Web.config. NOT in the file Front Matter (we're skipping the file in the following call)
            string basePath = FieldValuesHelper.GetFieldValue("TemplatesBasePath", defValue: "~/Templates/");
            return VirtualPathUtility.AppendTrailingSlash(basePath) + VirtualPathUtility.AppendTrailingSlash(templateName) + layoutName;
        }

        /// <summary>
        /// Reads a template from cache if available. If not, reads it frm disk.
        /// Substitutes the template fields such as {basefolder}, before caching the result
        /// </summary>
        /// <param name="filePath">Path to the template</param>
        /// <param name="ctx">The current request context (needed in in order to transform virtual paths)</param>
        /// <param name="isInclude">true to indicate that the current template is a fragment of other template, so that is excludes content and other fragments from processing</param>
        /// <returns>The text contents of the template</returns>
        /// <exception cref="System.Security.SecurityException">Thrown if the app has no read access to the requested file</exception>
        /// <exception cref="System.IO.FileNotFoundException">Thrown when the requested file does not exist</exception>
        private static string ReadTemplate(string templateVirtualPath, HttpContext ctx, List<string> cacheDependencies, List<string> graph = null, bool isInclude = false)
        {
            string templatePath = ctx.Server.MapPath(templateVirtualPath);
            string cachedContent = HttpRuntime.Cache[templatePath] as string;   //Templates are always cached for performance reasons (no switch/parameter to disable it)
            if (string.IsNullOrEmpty(cachedContent))
            {
                //Check for circular references
                if (graph != null)
                {
                    //Check if current file is already on the graph
                    //if it is, then we have a circular reference
                    if (graph.Contains(templatePath))
                    {
                        throw new CircularReferenceException( String.Format("Template not valid!\nThe file '{0}' is a circular reference: {1}", templateVirtualPath, String.Join(" >\n ", graph.ToArray())) );
                    }
                    graph.Add(templatePath);    //Add current include to the graph to track circular references
                }

                var templateContents = IOHelper.ReadTextFromFile(templatePath);  //Read template contents from disk

                //Add current file as cache dependency (the read process will add the fragments if needed)
                if (!cacheDependencies.Contains(templatePath))
                    cacheDependencies.Add(templatePath);

                string phValue = string.Empty;    //The value to substitute the placeholder

                ////////////////////////////////////////////
                //Search for includes in the current file and substitute them, before substituting any other placeholder
                ////////////////////////////////////////////
                string[] includes = TemplatingHelper.GetAllPlaceHolderNames(templateContents, "", FILE_INCLUDES_PREFIX);
                
                //Substitute includes with their contents
                foreach (string include in includes)
                {
                    string includeFileName = VirtualPathUtility.RemoveTrailingSlash(VirtualPathUtility.GetDirectory(templateVirtualPath)) + "/" + include.Substring(FILE_INCLUDES_PREFIX.Length);  //The current template file folder + the include filename
                    try
                    {
                        //Initialize graph to detect circular references
                        List<string> newGraph;
                        if (graph == null)
                        {
                            newGraph = new List<string>()
                            {
                                templatePath
                            };
                        }
                        else
                        {
                            newGraph = graph;
                        }
                        phValue = ReadTemplate(includeFileName, ctx, cacheDependencies, newGraph, true);    //Insert the raw contents of the include (no processing!)
                    }
                    catch(CircularReferenceException crex)
                    {
                        throw crex;
                    }
                    catch   //If it fails, simply do nothing
                    {
                        //TODO: log in the system log
                        phValue = String.Format("<!-- Include file '{0}' not found  -->", includeFileName);
                    }
                    templateContents = TemplatingHelper.ReplacePlaceHolder(templateContents, include, phValue);
                }


                if (!isInclude)
                {
                    //After inserting all the "includes", check if there's a content placeholder present (mandatory)
                    if ( !TemplatingHelper.IsPlaceHolderPresent(templateContents, "content") )
                    {
                        throw new Exception("Invalid template: The " + TemplatingHelper.GetPlaceholderName("content") + " placeholder must be present!");
                    }

                    //////////////////////////////
                    //Replace template-specific fields
                    //////////////////////////////
                    //Legacy "basefolder" placeholder (now "~/" it's recommended)
                    templateContents = TemplatingHelper.ReplacePlaceHolder(templateContents, "basefolder", "~/");
                    //Template base folder
                    templateContents = TemplatingHelper.ReplacePlaceHolder(templateContents, "templatebasefolder",
                        VirtualPathUtility.RemoveTrailingSlash(VirtualPathUtility.GetDirectory(VirtualPathUtility.ToAbsolute(templateVirtualPath))));

                    //Transform virtual paths into absolute to the root paths (This is done only once per file if cached)
                    templateContents = WebHelper.TransformVirtualPaths(templateContents);

                    //If it's the main file, add result to cache with dependency on the file(s)
                    //Templates are cached ALWAYS, and this is not dependent on the UseMDcaching parameter (that one is only for MarkDown or MDH files)
                    HttpRuntime.Cache.Insert(templatePath, templateContents, new CacheDependency(cacheDependencies.ToArray()));
                    //Keep a list of template's dependencies to reuse when not reading from cache
                    ctx.Application[templatePath] = cacheDependencies;
                }

                return templateContents; //Return content
            }
            else
            {
                //Get dependencies for this template
                cacheDependencies.AddRange(ctx.Application[templatePath] as List<string>);
                return cachedContent;   //Return directly from cache
            }
        }

        //Finds fragment placeholders and insert their contents
        private static string ProcessFragments(string template, MarkdownFile md, HttpContext ctx)
        {
            string[] fragments = TemplatingHelper.GetAllPlaceHolderNames(template, phPrefix: FILE_FRAGMENT_PREFIX);
            foreach(string fragmentName in fragments)
            {
                string fragmentContent = string.Empty;   //Default empty value
                string fragmentFileName = ctx.Server.MapPath(Path.GetFileNameWithoutExtension(md.FileName) + fragmentName.Substring(FILE_FRAGMENT_PREFIX.Length));  //Removing the "*" at the beggining
                //Test if a file the same file extension exists
                if (File.Exists(fragmentFileName + md.FileExt))
                    fragmentFileName += md.FileExt;
                else if (File.Exists(fragmentFileName + MarkdownFile.MARKDOWN_DEF_EXT)) //Try with .md extension
                    fragmentFileName += MarkdownFile.MARKDOWN_DEF_EXT;
                else
                    fragmentFileName += MarkdownFile.HTML_EXT; //Try with .mdh

                //Try to read the file with fragment
                try
                {
                    md.Dependencies.Add(fragmentFileName);

                    MarkdownFile mdFld = new MarkdownFile(fragmentFileName);
                    fragmentContent = mdFld.RawHTML;
                }
                catch
                {
                    //If something is wrong (normally the file does not exist) simply return an empty string
                    //We don't want to force this kind of files to always exist
                    fragmentContent = string.Empty;
                }
                //Replace the placeholder with the value
                template = TemplatingHelper.ReplacePlaceHolder(template, fragmentName, fragmentContent);
            }

            return template;
        }

#region Reflection methods
        //The Namespace that contains custom DotLiquid tags
        private const string CUSTOM_TAGS_NAMESPACE = "MIISHandler.Tags";
        //The Namespace that contains custom DotLiquid filters
        private const string CUSTOM_FILTERS_NAMESPACE = "MIISHandler.Filters";
        //The Namespace that contains custom DotLiquid Front-Matter Sources
        private const string CUSTOM_FMSOURCES_NAMESPACE = "MIISHandler.FMSources";
        
        //The Application variable that flags that the custom tags had beed added
        private const string MIIS_EXTENSIONS_ADDED_FLAG = "__MIISCustomExtensionsAdded";

        //Registers all custom Tags in the assembly passed as a parameter
        private static void RegisterCustomTagsInAssembly(Assembly assembly)
        {
            MethodInfo genericRegisterTag = typeof(Template).GetMethod("RegisterTag"); //Needed to call it as a generic for each tag using reflection

            //Get all custom tags in the MIISHandler.Tags namespace
            var tags = from c in assembly.GetTypes()
                       where c.IsClass && c.Namespace == CUSTOM_TAGS_NAMESPACE 
                             && (c.IsSubclassOf(typeof(Tag)) || c.IsSubclassOf(typeof(Block)))
                       select c;
            //Register each tag
            tags.ToList().ForEach(tag =>
                {
                    //This would be the normal, non-reflection way to do it: Template.RegisterTag<TagClass>("tagclassname");
                    MethodInfo registerTag = genericRegisterTag.MakeGenericMethod(tag);
                    registerTag.Invoke(null, new object[] { tag.Name.ToLower() });
                });
        }

        //Registers all custom filters in the assembly passed as a parameter
        private static void RegisterCustomFiltersInAssembly(Assembly assembly)
        {
            //Custom filters are obtained from classes in the Filters namespace that implement the IFilterFactory interface
            var filterFactories = from c in assembly.GetTypes()
                       where c.IsClass && c.Namespace == CUSTOM_FILTERS_NAMESPACE && (typeof(IFilterFactory)).IsAssignableFrom(c)
                       select c;
            //Register each filter globally using its factory method (GetFilterType)
            filterFactories.ToList().ForEach( filterFactoryClass =>
                {
                    IFilterFactory ff = (IFilterFactory) Activator.CreateInstance(filterFactoryClass);
                    Template.RegisterFilter(ff.GetFilterType());
                });
        }

        //Registers all custom Front-Matter sources inside the assembly passed as a parameter
        private static void RegisterCustomFMSourcesInAssembly(Assembly assembly)
        {
            //Custom FM sources are obtained from classes in the FMSources namespace that implement the IFMSource interface
            var fmSources = from c in assembly.GetTypes()
                                  where c.IsClass && c.Namespace == CUSTOM_FMSOURCES_NAMESPACE && (typeof(IFMSource)).IsAssignableFrom(c)
                                  select c;
            //Register each FMSource globally using its factory method (GetFilterType)
            fmSources.ToList().ForEach(fmSourceClass =>
            {
                IFMSource fms = (IFMSource)Activator.CreateInstance(fmSourceClass);
                FieldValuesHelper.AddFrontMatterSource(fms.SourceName, fms.GetType());
            });
        }

        //Loads all of the Tags, Fields and custom FM sources from the asemblies in the "Bin" folder
        //Source: https://stackoverflow.com/a/5599581/4141866
        private static void LoadAndRegisterCustomExtensionsInAssemblies()
        {
            //Path to the bin folder
            string binPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "bin");
            //The names of the assemblies currently referenced in the app (to prevent checking them for custom Tags)
            AssemblyName[] referencedAssemblies = Assembly.GetExecutingAssembly().GetReferencedAssemblies();
            List<string> referencedAssembliesNamesList = referencedAssemblies.Select(ra => ra.FullName).ToList<string>();

            foreach (string dll in Directory.GetFiles(binPath, "*.dll", SearchOption.AllDirectories))   //Including subdirectories, so you can add the custom tags in subfolders
            {
                try
                {
                    Assembly loadedAssembly = Assembly.LoadFile(dll);
                    //If it's not an assembly referenced by the main app (the main app is not in the list, so it's also checked and loaded)
                    if (referencedAssembliesNamesList.IndexOf(loadedAssembly.FullName) < 0)
                    {
                        RegisterCustomTagsInAssembly(loadedAssembly);
                        RegisterCustomFiltersInAssembly(loadedAssembly);
                        RegisterCustomFMSourcesInAssembly(loadedAssembly);
                    }
                }
                catch (FileLoadException)
                { } // The Assembly has already been loaded.
                catch (BadImageFormatException)
                { } // If a BadImageFormatException exception is thrown, the file is not an assembly.
            }
        }
        
        //Registers all the custom Liquid tags and filters in the project. It will do it only the first time
        private static void RegisterCustomExtensions()
        {
            HttpApplicationState app = HttpContext.Current.Application;
            if (app[MIIS_EXTENSIONS_ADDED_FLAG] == null)   //Tags had not been added before
            {
                app.Lock(); //Prevent parallel request to add the tags twice
                try
                {
                    //Load Custom Tag assemblies and register them
                    LoadAndRegisterCustomExtensionsInAssemblies();
                    app[MIIS_EXTENSIONS_ADDED_FLAG] = 1;   //Anything in the value would do to flag that Tags have been inserted
                }
                catch
                {
                    //retrow the exception
                    throw;
                }
                finally
                {
                    app.UnLock();   //Ensure application state is not locked
                }
            }
        }
#endregion
#endregion

    #region Aux classes
    internal class CircularReferenceException : Exception
        {
            public CircularReferenceException(string msg):base(msg)
            {
            }
        }
        #endregion
    }
}
