using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Caching;
using IISHelpers;

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

        #region Methods
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
            if (!String.IsNullOrEmpty(templateFile))
            {
                template = ReadTemplate(templateFile, ctx);    //Read, transform and cache template
            }

            //First process the "content" field with the main HTML content transformed from Markdown
            //This allows to use other fields inside the content itself, not only in the templates
            template = TemplatingHelper.ReplacePlaceHolder(template, "content", md.RawHTML);

            //Process well-known fields one by one
            template = TemplatingHelper.ReplacePlaceHolder(template, "title", md.Title);
            template = TemplatingHelper.ReplacePlaceHolder(template, "filename", md.FileName);
            template = TemplatingHelper.ReplacePlaceHolder(template, "datecreated", md.DateCreated.ToString());
            template = TemplatingHelper.ReplacePlaceHolder(template, "datemodified", md.DateLastModified.ToString());
            template = TemplatingHelper.ReplacePlaceHolder(template, "isauthenticated", ctx.User.Identity.IsAuthenticated.ToString());
            template = TemplatingHelper.ReplacePlaceHolder(template, "authtype", ctx.User.Identity.AuthenticationType);
            template = TemplatingHelper.ReplacePlaceHolder(template, "username", ctx.User.Identity.Name);

            //Process fragments (other files inserted into the current one or template)
            template = ProcessFragments(template, md, ctx);

            //Process custom fields
            template = ProcessCustomFields(template, md, ctx);

            //Return the transformed file
            return template;
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
            string templateName = Common.GetFieldValue("TemplateName", md);
            if (string.IsNullOrEmpty(templateName))
                return string.Empty;    //Use the default basic HTML5 template

            //The name (or sub-path) for the layout file (.html normaly) to be used
            string layoutName = Common.GetFieldValue("Layout", md);
            if (string.IsNullOrEmpty(layoutName))
                return string.Empty;    //Use the default basic HTML5 template

            //If both the template folder and the layout are established, then get the base folder for the templates
            //This base path for the templates parameter is only available through Web.config. NOT in the file Front Matter (we're skipping the file in the following call)
            string basePath = Common.GetFieldValue("TemplatesBasePath", defValue: "~/Templates/");
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
        private static string ReadTemplate(string templateVirtualPath, HttpContext ctx, bool isInclude = false)
        {
            string templatePath = ctx.Server.MapPath(templateVirtualPath);
            string cachedContent = HttpRuntime.Cache[templatePath] as string;   //Templates are always cached for performance reasons (no switch parameter to disable it)
            if (string.IsNullOrEmpty(cachedContent))
            {
                var templateContents = IOHelper.ReadTextFromFile(templatePath);  //Read template contents from disk

                //If it's an include, just return the raw contents 
                //Placeholders are porcessed in the main template
                //Tke into account that sub-includes are not allowed in includes to prevent circular references, so they won't be processed either (which is OK)
                if (isInclude)
                    return templateContents;

                //Init the cache dependencies list
                List<string> cacheDependencies = new List<string>
                {
                    templatePath   //Add current file as cache dependency (the read process will add the fragments if needed)
                };

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
                        phValue = ReadTemplate(includeFileName, ctx, true);    //Insert the raw contents of the include (no processing!)
                    }
                    catch   //If it fails, simply do nothing
                    {
                        //TODO: log in the system log
                        phValue = String.Format("<!-- Include file '{0}' not found  -->", includeFileName);
                    }
                    templateContents = TemplatingHelper.ReplacePlaceHolder(templateContents, include, phValue);
                    cacheDependencies.Add(ctx.Server.MapPath(includeFileName)); //Add "include" file to the cache dependencies for the main template file cached html
                }

                //After inserting all he "includes", check if there's a content placeholder present (mandatory)
                if (!TemplatingHelper.IsPlaceHolderPresent(templateContents, "content"))
                {
                    throw new Exception("Invalid template: The " + TemplatingHelper.GetPlaceholderName("content") + " placeholder must be present!");
                }

                //////////////////////////////
                //Replace template-specific fields
                //////////////////////////////
                //Legacy "basefolder" placeholder (now "~/" it's recommended)
                templateContents = TemplatingHelper.ReplacePlaceHolder(templateContents, "basefolder", 
                    VirtualPathUtility.RemoveTrailingSlash(VirtualPathUtility.ToAbsolute("~/")));
                //Template base folder
                templateContents = TemplatingHelper.ReplacePlaceHolder(templateContents, "templatebasefolder",
                    VirtualPathUtility.RemoveTrailingSlash(VirtualPathUtility.GetDirectory(VirtualPathUtility.ToAbsolute(templateVirtualPath))));

                //Transform virtual paths into absolute to the root paths (This is done only once per file if cached)
                templateContents = WebHelper.TransformVirtualPaths(templateContents);

                //Add result to cache with dependency on the file(s)
                HttpRuntime.Cache.Insert(templatePath, templateContents, new CacheDependency(cacheDependencies.ToArray()));

                return templateContents; //Return content
            }
            else
                return cachedContent;   //Return directly from cache
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
                else if (File.Exists(fragmentFileName + ".md")) //Try with .md extension
                    fragmentFileName += ".md";
                else
                    fragmentFileName += ".mdh"; //Try with .mdh

                //Try to read the file with fragment
                try
                {
                    if (md.Dependencies != null)
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

        //Takes care of custom fields such as Front Matter Properties and custom default values in web.config
        private static string ProcessCustomFields(string template, MarkdownFile md, HttpContext ctx)
        {
            string[] names = TemplatingHelper.GetAllPlaceHolderNames(template);
            foreach (string name in names)
            {
                //Get current value for the field, from Front Matter or web.config
                string fldVal = Common.GetFieldValue(name, md);
                /*
                 * There are two types of fields:
                 * - Value fields: {name} -> Get a value from the properties of the file or from web.config -> Simply replace them
                 * - File processing fields (FPF), ending in .md or .mdh. ej: {{myfile.md}} -> The file is read and it's contents transformed into HTML take the place of the placeholder
                 *   Useful for menus, and other independet parts in custom templates and parts of the same page.
                */
                if (fldVal.EndsWith(".md") || fldVal.EndsWith(MarkdownFile.HTML_EXT))
                {
                    try
                    {
                        string fpfPath = ctx.Server.MapPath(fldVal);    //The File-Processing Field path
                        MarkdownFile mdFld = new MarkdownFile(fpfPath);
                        fldVal = mdFld.RawHTML; //Use the raw HTML, not the processed HTML (this last one includes the template too)
                        //Add the processed file to the dependencies of the currently processed content file, so that the file is invalidated when the FPF changes
                        md.Dependencies.Add(fpfPath);
                    }
                    catch (SecurityException)
                    {
                        fldVal = String.Format("Can't access file for {0}", name);
                    }
                    catch (FileNotFoundException)
                    {
                        fldVal = String.Format("File not found for {0}", name);
                    }
                    catch (Exception ex)
                    {
                        fldVal = String.Format("Error loading {0}: {1}", fldVal, ex.Message);   //This should only happen while testing, never in production, so I send the exception's message
                    }
                }
                else if (fldVal.StartsWith("~/"))    //If its a virtual path to a static file (for example a path to a CSS or JS file)
                {
                    //Convert relative path to relative URL from the root (changes the "~/" for the root path 
                    //of the application. Needed if the current handler is running as a virtual app in IIS)
                    fldVal = VirtualPathUtility.ToAbsolute(fldVal);
                    //There's no need to transform any other virtual path because this is done (and cached) on every file the first time is retrieved and transformed
                }
                template = TemplatingHelper.ReplacePlaceHolder(template, name, fldVal);
            }

            return template;
        }
        #endregion
    }
}