using System;
using System.IO;
using System.Security;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;

namespace IISMarkdownHandler
{
    /// <summary>
    /// Renders the final HTML from Markdown using the spcified template or CSS file
    /// </summary>
    public class HTMLRenderer
    {
        //Current request context
        private HttpContext ctx = null;
        //This is simply a plain HTML5 file to show the contents inside if there's no template specified, 
        //to ensure at least a valid HTML5 page returned and not just a bunch of HTML tags
        private const string DEFAULT_TEMPLATE =
@"<!doctype html>
<html>
<head>
<title>{title}</title>
<link rel=""stylesheet"" href=""{cssfile}"">
</head>
<body>
{content}
</body>
</html>";

        #region Constructor
        public HTMLRenderer(HttpContext currentContext)
        {
            //Needed to find the template files relative paths
            ctx = currentContext;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Renders the HTML from the markdown using the templates and parameters specified in web.config
        /// and processing the templates
        /// </summary>
        /// <param name="md">The markdown file information</param>
        /// <returns>The final HTML to return to the client</returns>
        public string RenderMarkdown(MarkdownFile md)
        {
            string templateFile = WebConfigurationManager.AppSettings["Markdown-Template"];
            string template = DEFAULT_TEMPLATE; //The default template for the final HTML
            if( !String.IsNullOrEmpty(templateFile) )
            {
                template = Helper.readTemplate(templateFile, ctx);    //Read, transform and cache template
            }

            //////////////////////////////
            /*
             * Process the fields in the template.
             * There are two types of fields:
             * - Value fields: {name} -> Get a value from the properties of the Markdown file or from web.config
             * - File processing fields: {file.md} -> The file is read and it's contents, transformed to HTML, replace the field. 
             *   Useful for menus, and other independet parts in custom templates.
            */
            //////////////////////////////

            foreach (Match field in Helper.REGEXFIELDS.Matches(template))
            {
                //Get the field name (without braces and in lowercase)
                string name = field.Value.Substring(1, field.Value.Length-2).Trim().ToLower();
                string fldVal = "";
                switch(name)
                {
                    //First well-known fields
                    case "content": //Main HTML content transformed from Markdown
                        fldVal = md.HTML;
                        break;
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
                    default:
                        //Try to read from web.config
                        fldVal = WebConfigurationManager.AppSettings[name];
                        if (!String.IsNullOrEmpty(fldVal))  //If a value is found for the parameter
                        {
                            fldVal = fldVal.Trim();
                            //If it ends in .md, we must inject the contents as the real value
                            if (fldVal.EndsWith(".md"))
                            {
                                try
                                {
                                    MarkdownFile mdFld = new MarkdownFile(ctx.Server.MapPath(fldVal));
                                    fldVal = mdFld.HTML;
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
                                    fldVal = String.Format("Error loading {0}: {1}", fldVal, ex.Message);
                                }
                            }
                            else if (fldVal.StartsWith("~/"))    //If its a path to a file relative to the root (for example a path to a CSS or JS file)
                            {
                                //Convert relative path to relative URL from the root (changes the "~" for the root path 
                                //of the application. Needed if the current handler is running as a virtual app in IIS )
                                fldVal = ctx.Request.ApplicationPath + fldVal.Substring(2);
                            }
                            //If it was a non-file parameter, simply use the retrieved value from config (nohing to be done)
                        }
                        break;
                }
                template = template.Replace(field.Value, fldVal);
            }

            return template;
        }

        #endregion

    }
}