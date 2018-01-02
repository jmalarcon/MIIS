using System;
using System.IO;
using System.Security;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Caching;

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
            string templateFile = Helper.GetParamValue("Markdown-Template");
            string template = DEFAULT_TEMPLATE; //The default template for the final HTML
            if( !String.IsNullOrEmpty(templateFile) )
            {
                template = Helper.readTemplate(templateFile, ctx);    //Read, transform and cache template
            }

            //First process the "content" field with the main HTML content transformed from Markdown
            //This allows to use other fields inside the content itself, not only in the templates
            template = Helper.ReplacePlaceHolder(template, "content", md.RawHTML);

            //////////////////////////////
            /*
             * Now process the fields in the template or file.
             * There are two types of fields:
             * - Value fields: {name} -> Get a value from the properties of the file or from web.config
             * - File processing fields: {file.md(h)} or {*-file.md(h)} -> The file is read and it's contents, transformed to HTML, replace the field. 
             *   Useful for menus, and other independet parts in custom templates and parts of the same page.
            */
            //////////////////////////////

            foreach (Match field in Helper.REGEXFIELDS_PATTERN.Matches(template))
            {
                //Get the field name (without braces and in lowercase)
                string name = Helper.GetFieldName(field.Value);
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
                //template = Helper.ReplacePlaceHolder(template, name, fldVal);
            }

            //Return the transformed file
            return template;
        }

        #endregion

        #region Aux methods
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
                fldVal = Helper.GetParamValue(name).Trim();


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