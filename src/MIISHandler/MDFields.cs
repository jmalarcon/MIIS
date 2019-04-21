using System;
using System.Web;
using DotLiquid;
using IISHelpers;

namespace MIISHandler
{
    /// <summary>
    /// This class allows to retrieve field values dynamically for DotLiquid (just when they're needed)
    /// </summary>
    internal class MDFieldsResolver : Hash
    {
        private MarkdownFile md;
        private HttpContext ctx;

        //Constructor
        public MDFieldsResolver(MarkdownFile mdFile, HttpContext context)
        {
            md = mdFile;
            ctx = context;
        }

        //Retrieves the value for the specified field or returns "" if it doesn't exist
        protected override object GetValue(string name)
        {
            string res = "";    //Default value
            switch (name.ToLower())
            {
            //Check well Known fields first
                case "title":
                    res = md.Title;
                    break;
                case "filename":
					res = md.FileName;
					break;
                case "datecreated":
                    res = md.DateCreated.ToString();
					break;
                case "datemodified":
                    res = md.DateLastModified.ToString();
					break;
                case "isauthenticated":
                    res = ctx.User.Identity.IsAuthenticated.ToString();
					break;
                case "authtype":
                    res = ctx.User.Identity.AuthenticationType;
					break;
                case "username":
                    res = ctx.User.Identity.Name;
					break;
                case "domain":
                    res = ctx.Request.Url.Authority;
					break;
                case "baseurl":
                    res = $"{ctx.Request.Url.Scheme}{System.Uri.SchemeDelimiter}{ctx.Request.Url.Authority}";
					break;
                case "now":
                    res = DateTime.Now.ToString();
					break;
                case "time":
                    res = DateTime.Now.ToLongTimeString();
					break;
                case "url":
                    res = ctx.Request.Url.AbsolutePath;
					break;
                case "noexturl":
                    //Files processed by MIIS always have extension on disk
                    res = ctx.Request.Path.Remove(ctx.Request.Path.LastIndexOf("."));
					break;
            //Custom fields
            default:
                    res = Common.GetFieldValue(name, md);
                    /*
                     * There are two types of fields:
                     * - Value fields: {{name}} -> Get a value from the Front-Matter or from web.config -> Simply replace them
                     * - File processing fields (FPF), ending in .md or .mdh. ej: {{myfile.md}} -> The file is read and it's contents transformed into HTML take the place of the placeholder
                     *   Useful for menus, and other independet parts in custom templates and parts of the same page.
                    */
                    if (res.EndsWith(".md") || res.EndsWith(MarkdownFile.HTML_EXT))
                    {
                        try
                        {
                            string fpfPath = ctx.Server.MapPath(res);    //The File-Processing Field path
                            MarkdownFile mdFld = new MarkdownFile(fpfPath);
                            res = mdFld.RawHTML; //Use the raw HTML, not the processed HTML (this last one includes the template too)
                                                    //Add the processed file to the dependencies of the currently processed content file, so that the file is invalidated when the FPF changes (if caching is enabled)
                            md.Dependencies.Add(fpfPath);
                        }
                        catch (System.Security.SecurityException)
                        {
                            res = String.Format("Can't access file for {0}", TemplatingHelper.PLACEHOLDER_PREFIX + name + TemplatingHelper.PLACEHOLDER_SUFIX);
                        }
                        catch (System.IO.FileNotFoundException)
                        {
                            res = String.Format("File not found for {0}", TemplatingHelper.PLACEHOLDER_PREFIX + name + TemplatingHelper.PLACEHOLDER_SUFIX);
                        }
                        catch (Exception ex)
                        {
                            //This should only happen while testing, never in production, so I send the exception's message
                            res = String.Format("Error loading {0}: {1}", TemplatingHelper.PLACEHOLDER_PREFIX + name + TemplatingHelper.PLACEHOLDER_SUFIX, ex.Message);
                        }
                    }
                    break;
            }

            return res;
        }
    }
}