using System;
using System.IO;
using System.Web;
using System.Web.Caching;
using System.Text.RegularExpressions;

namespace IISMarkdownHandler
{
    public static class Helper
    {
        //The regular expression to find fields in templates
        internal static Regex REGEXFIELDS = new Regex(@"\{[a-z]+?\}");


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
                        case "content": //Main HTML content transformed from Markdown
                            if (ContentPresent) //Only one {content} placeholder can be present
                                throw new Exception("Invalid template: The {content} placeholder can be only used once in a template!");
                            ContentPresent = true;
                            continue;   //This is a check only, no transformation of {content} needed at this point
                        case "basefolder":  //base folder of current web app in IIS
                            if (basefolder == "")
                                basefolder = VirtualPathUtility.ToAbsolute("~/");   //Just once per template
                            fldVal = basefolder;
                            break;
                        case "templatebasefolder":  //Base folder of the current template
                            if (templatebasefolder == "")
                                templatebasefolder = VirtualPathUtility.GetDirectory(VirtualPathUtility.ToAbsolute(templateVirtualPath)); //Just once per template
                            fldVal = templatebasefolder;
                            break;
                        default:
                            continue;   //Any  field not in the previous cases gets ignored (they must be processed with a Markdown file
                    }
                    templateContents = templateContents.Replace(field.Value, fldVal);
                }
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