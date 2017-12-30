using System;
using System.IO;
using System.Web;
using System.Web.Configuration;
using System.Web.Caching;
using System.Text.RegularExpressions;

namespace MIISHandler
{
    public static class Helper
    {
        //The regular expression to find fields in templates
        internal static Regex REGEXFIELDS = new Regex(@"\{[0-9A-Za-z_]+?\}");

        //Returns a param from web.config or a default value for it
        //The defaultValue can be skipped and it will be returned an empty string if it's needed
        internal static string GetParamValue(string paramName, string defaultvalue = "")
        {
            string v = WebConfigurationManager.AppSettings[paramName];
            return String.IsNullOrEmpty(v) ? defaultvalue : v.Trim();
        }

        //Tries to convert any object to the specified type
        internal static T DoConvert<T>(object v)
        {
            try
            {
                return (T)Convert.ChangeType(v, typeof(T));
            }
            catch
            {
                return (T)Activator.CreateInstance(typeof(T));
            }
        }

        /// <summary>
        /// Tries to read the requested file from disk and returns the contents
        /// </summary>
        /// <param name="requestPath">Path to the file</param>
        /// <returns>The text contents of the file</returns>
        /// <exception cref="System.Security.SecurityException">Thrown if the app has no read access to the requested file</exception>
        /// <exception cref="System.IO.FileNotFoundException">Thrown when the requested file does not exist</exception>
        internal static string readTextFromFile(string filePath)
        {
            //The route of the file we need to process
            using (StreamReader srMD = new StreamReader(filePath))
            {
                return srMD.ReadToEnd(); //Text file contents
            }
        }

        /// <summary>
        /// Reads a file from cache if available. If not, reads it from disk.
        /// If read from disk it adds the results to the cache with a dependency on the file 
        /// so that, if the file changes, the cache is immediately invalidated and the new changes read from disk.
        /// </summary>
        /// <param name="requestPath">Path to the file</param>
        /// <returns>The text contents of the file</returns>
        /// <exception cref="System.Security.SecurityException">Thrown if the app has no read access to the requested file</exception>
        /// <exception cref="System.IO.FileNotFoundException">Thrown when the requested file does not exist</exception>
        internal static string readTextFromFileWithCaching(string filePath)
        {
            string cachedContent = HttpRuntime.Cache[filePath] as string;
            if (string.IsNullOrEmpty(cachedContent))
            {
                string content = readTextFromFile(filePath);    //Read file contents from disk
                HttpRuntime.Cache.Insert(filePath, content, new CacheDependency(filePath)); //Add result to cache with dependency on the file
                return content; //Return content
            }
            else
                return cachedContent;   //Return directly from cache
        }

        /// <summary>
        /// Searchs for virtual paths ("~/") and transform them to absolute paths (relative to the root of the server)
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        internal static string TransformVirtualPaths(string content)
        {
            string absoluteBase = VirtualPathUtility.ToAbsolute("~/");
            content = content.Replace("~/", absoluteBase);
            //Markdig codifies the "~" as "%7E" , so we need to process it this way too
            return content.Replace("%7E/", absoluteBase);
        }

        /// <summary>
        /// Reads a template from cache if available. If not, reads it frm disk.
        /// Substitutes the template fields such as {basefolder}, before caching the result
        /// </summary>
        /// <param name="filePath">Path to the template</param>
        /// <param name="ctx">The current request context (needed in in order to transform virtual paths)</param>
        /// <returns>The text contents of the template</returns>
        /// <exception cref="System.Security.SecurityException">Thrown if the app has no read access to the requested file</exception>
        /// <exception cref="System.IO.FileNotFoundException">Thrown when the requested file does not exist</exception>
        internal static string readTemplate(string templateVirtualPath, HttpContext ctx)
        {
            string templatePath = ctx.Server.MapPath(templateVirtualPath);
            string cachedContent = HttpRuntime.Cache[templatePath] as string;
            if (string.IsNullOrEmpty(cachedContent))
            {
                var templateContents = readTextFromFile(templatePath);  //Read template contents from disk
                //////////////////////////////
                //Replace template fields
                //////////////////////////////
                bool ContentPresent = false;
                string basefolder = "", templatebasefolder = "";
                foreach (Match field in Helper.REGEXFIELDS.Matches(templateContents))
                {
                    //Get the field name (without braces and in lowercase)
                    string name = field.Value.Substring(1, field.Value.Length - 2).Trim().ToLower();
                    string fldVal = "";
                    switch (name)
                    {
                        case "content": //Main HTML content transformed from Markdown, just checks if it's present because is mandatory. The processing is done later on each file
                            if (ContentPresent) //Only one {content} placeholder can be present
                                throw new Exception("Invalid template: The {content} placeholder can be only used once in a template!");
                            ContentPresent = true;
                            continue;   //This is a check only, no transformation of {content} needed at this point
                        case "basefolder":  //base folder of current web app in IIS - This is no longer needed since 1.2, because you can simply use ~/ for the same effect
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
                            continue;   //Any  field not in the previous cases gets ignored (they must be processed within a Markdown file)
                    }
                    templateContents = templateContents.Replace(field.Value, fldVal);
                }

                //Transform virtual paths into absolute to the root paths (This is done only once per file if cached)
                templateContents = Helper.TransformVirtualPaths(templateContents);

                //The {content} placeholder must be present or no Markdown contents can be shown
                if (!ContentPresent)
                    throw new Exception("Invalid template: The {content} placeholder must be present!");

                HttpRuntime.Cache.Insert(templatePath, templateContents, new CacheDependency(templatePath)); //Add result to cache with dependency on the file
                return templateContents; //Return content
            }
            else
                return cachedContent;   //Return directly from cache
        }
    }
}