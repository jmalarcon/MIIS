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
            if( !String.IsNullOrEmpty(templateFile) )
            {
                template = ReadTemplate(templateFile, ctx);    //Read, transform and cache template
            }

            //First process the "content" field with the main HTML content transformed from Markdown
            //This allows to use other fields inside the content itself, not only in the templates
            template = TemplatingHelper.ReplacePlaceHolder(template, "content", md.RawHTML);

            //////////////////////////////
            /*
             * Now process the fields in the template or file.
             * There are two types of fields:
             * - Value fields: {name} -> Get a value from the properties of the file or from web.config
             * - File processing fields: {file.md(h)} or {*-file.md(h)} -> The file is read and it's contents, transformed to HTML, replace the field. 
             *   Useful for menus, and other independet parts in custom templates and parts of the same page.
            */
            //////////////////////////////

            foreach (Match field in TemplatingHelper.REGEXFIELDS_PATTERN.Matches(template))
            {
                //Get the field name (without braces and in lowercase)
                string name = TemplatingHelper.GetFieldName(field.Value);
                string fldVal = "";
                switch(name)
                {
                    //First the well-known fields
                    case "title":   //Markdown file title
                        fldVal = md.Title;
                        break;
                    case "filename":    //Markdown filename
                        fldVal = md.FileName;
                        break;
                    case "datecreated": //Markdown file date created
                        fldVal = md.DateCreated.ToString();
                        break;
                    case "datemodified": //Markdown file date modified
                        fldVal = md.DateLastModified.ToString();
                        break;
                    case "isauthenticated":    //Is current user authenticated?
                        fldVal = ctx.User.Identity.IsAuthenticated.ToString();
                        break;
                    case "authtype":    //Authentication type
                        fldVal = ctx.User.Identity.AuthenticationType;
                        break;
                    case "username":    //Current authenticated user's name
                        fldVal = ctx.User.Identity.Name;
                        break;
                    default:
                        fldVal = ProcessCustomField(name, md);
                        break;
                }
                //Replace the raw placeholder (as is matched by the regular expression, not the lowercase version) with the value
                template = template.Replace(field.Value, fldVal);
            }

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
        /// <param name="isFragment">true to indicate that the current template is a fragment of other template, so that is excludes content and other fragments from processing</param>
        /// <returns>The text contents of the template</returns>
        /// <exception cref="System.Security.SecurityException">Thrown if the app has no read access to the requested file</exception>
        /// <exception cref="System.IO.FileNotFoundException">Thrown when the requested file does not exist</exception>
        private static string ReadTemplate(string templateVirtualPath, HttpContext ctx, bool isFragment = false)
        {
            string templatePath = ctx.Server.MapPath(templateVirtualPath);
            string cachedContent = HttpRuntime.Cache[templatePath] as string;
            if (string.IsNullOrEmpty(cachedContent))
            {
                var templateContents = IOHelper.ReadTextFromFile(templatePath);  //Read template contents from disk
                                                                                 //Init the cache dependencies list
                List<string> cacheDependencies = new List<string>
                {
                    templatePath   //Add current file as cache dependency (the read process will add the fragments if needed)
                };

                //////////////////////////////
                //Replace template fields
                //////////////////////////////
                bool ContentPresent = false;
                string basefolder = "", templatebasefolder = "";
                foreach (Match field in TemplatingHelper.REGEXFIELDS_PATTERN.Matches(templateContents))
                {
                    //Get the field name (without prefix or suffix and in lowercase)
                    string name = TemplatingHelper.GetFieldName(field.Value);
                    string fldVal = "";
                    switch (name)
                    {
                        case "content": //Main HTML content transformed from Markdown, just checks if it's present because is mandatory. The processing is done later on each file
                            if (isFragment) break; //Don't process content with fragments
                            if (ContentPresent) //Only one {content} placeholder can be present 
                                throw new Exception("Invalid template: The " + TemplatingHelper.FIELD_PREFIX + "content" + TemplatingHelper.FIELD_SUFIX + " placeholder can be only used once in a template!");
                            ContentPresent = true;
                            continue;   //This is a check only, no transformation of {content} needed at this point
                        case "basefolder":  //base folder of current web app in IIS - This is no longer needed, because you can simply use ~/ for the same effect
                            if (basefolder == "")
                                basefolder = VirtualPathUtility.ToAbsolute("~/");   //Just once per template
                            fldVal = VirtualPathUtility.RemoveTrailingSlash(basefolder);    //No trailing slash
                            break;
                        case "templatebasefolder":  //Base folder of the current template
                            if (templatebasefolder == "")
                                templatebasefolder = VirtualPathUtility.GetDirectory(VirtualPathUtility.ToAbsolute(templateVirtualPath)); //Just once per template
                            fldVal = VirtualPathUtility.RemoveTrailingSlash(templatebasefolder);    //No trailing slash
                            break;
                        default:
                            if (!isFragment && name.StartsWith("$"))
                            {
                                //string includeFileName = Path.Combine(Path.GetDirectoryName(ctx.Server.MapPath(templatePath)), name.Substring(1));  //The current template file folder + the include filename
                                string includeFileName = VirtualPathUtility.RemoveTrailingSlash(VirtualPathUtility.GetDirectory(templateVirtualPath)) + "/" + name.Substring(1);  //The current template file folder + the include filename
                                try
                                {
                                    fldVal = ReadTemplate(includeFileName, ctx, true);    //Insert the raw contents of the include (no processing!)
                                    cacheDependencies.Add(ctx.Server.MapPath(includeFileName)); //Add "import" file to the cache for the main template file results
                                    break;
                                }
                                catch   //If it fails, simply do nothing
                                {
                                }   
                            }
                            //Continue with the loop, skip the substitution.
                            continue;   //Any  field not in the previous cases is ignored, so no substitution (they must be processed within the file contents)
                    }
                    //Do the field substitution
                    templateContents = templateContents.Replace(field.Value, fldVal);
                }

                //Transform virtual paths into absolute to the root paths (This is done only once per file if cached)
                templateContents = WebHelper.TransformVirtualPaths(templateContents);

                //The {content} placeholder must be present or no Markdown contents can be shown
                if ( !(isFragment || ContentPresent))
                    throw new Exception("Invalid template: The " + TemplatingHelper.FIELD_PREFIX + "content" + TemplatingHelper.FIELD_SUFIX + " placeholder must be present!");

                //Add result to cache with dependency on the file(s)
                HttpRuntime.Cache.Insert(templatePath, templateContents, new CacheDependency(cacheDependencies.ToArray()));

                return templateContents; //Return content
            }
            else
                return cachedContent;   //Return directly from cache
        }

        //Takes care of custom fields such as Front Matter Properties and custom default values in web.config
        private static string ProcessCustomField(string name, MarkdownFile md)
        {
            string fldVal = string.Empty;   //Default empty value
            HttpContext ctx = HttpContext.Current;

            ///// FRAGMENTS

            //If the field name starts with "*" then it's a placeholder for a file complementary to the current one (a "fragment": header, sidebar...) One, per main file.
            if (name.StartsWith("*"))
            {
                string fragmentFileName = ctx.Server.MapPath(Path.GetFileNameWithoutExtension(md.FileName) + name.Substring(1));  //Removing the "*" at the beggining
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
                    fldVal = mdFld.RawHTML;
                }
                catch
                {
                    //If something is wrong (normally the file does not exist) simply return an empty string
                    //We don't want to force this kind of files to always exist
                    fldVal = "";
                }
                return fldVal;
            }

            //////CUSTOM FIELD NAMES

            //Any other field...
            //Try to get value from Front Matter...
            fldVal = md.FrontMatter[name];
            //If it's not in the FM, then try to get it from web.config for default values
            if (string.IsNullOrEmpty(fldVal))
                fldVal = WebHelper.GetParamValue(name).Trim();


            if (!String.IsNullOrEmpty(fldVal))  //If a value is found for the parameter
            {
                //If it ends in .md or .mdh (the extension for HTML-only content files), we must inject the contents as the real value
                if (fldVal.EndsWith(".md") || fldVal.EndsWith(MarkdownFile.HTML_EXT))
                {
                    try
                    {
                        MarkdownFile mdFld = new MarkdownFile(ctx.Server.MapPath(fldVal));
                        fldVal = mdFld.RawHTML;
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
            }

            return fldVal;
        }
        #endregion
    }
}