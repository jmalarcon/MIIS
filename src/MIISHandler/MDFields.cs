using System;
using System.Linq;
using System.Web;
using DotLiquid;
using IISHelpers;

namespace MIISHandler
{
    /// <summary>
    /// This class allows to retrieve field values dynamically/lazyly for DotLiquid (just when they're needed)
    /// </summary>
    public class MDFieldsResolver : Hash
    {
        //This is an internal field accesible only through custom tags, that gives access to this resolver
        public const string INTERNAL_REFERENCE_TO_CURRENT_FILE = "_currentfileproxy";
        //The delimiter that signals custom Front-Matter sources for fields that generate information dynamically
        private const string FRONT_MATTER_SOURCES_PREFIX = "!!";

        private readonly MarkdownFile md;
        private readonly HttpContext ctx;

        //Constructor
        public MDFieldsResolver(MarkdownFile mdFile, HttpContext context)
        {
            md = mdFile;
            ctx = context;
        }

        //Retrieves the value for the specified field or returns an empty string if it doesn't exist
        protected override object GetValue(string name)
        {
            object res = "";
            switch (name.ToLower())
            {
                //Check well Known fields first
                case INTERNAL_REFERENCE_TO_CURRENT_FILE:
                    //This is intended to be used internally only, from custom tags
                    res = new MIISFile(md);
                    break;
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
                    string sres = FieldValuesHelper.GetFieldValue(name, md);
                    /*
                     * There are three types of fields:
                     * - File processing fields (FPF), ending in .md or .mdh. ej: myfile.md -> The file is read and it's contents transformed into HTML take the place of the placeholder
                     *   Useful for menus, and other independet parts in custom templates and parts of the same page.
                     * - Custom Dinamic Field Sources, that start with !! and use a custom class to populate the field with an object. Ej: !!customSource param1 param2
                     * - Value fields: {{name}} -> Get a value from the Front-Matter or from web.config -> Simply replace them
                    */
                    if (sres.ToLower().EndsWith(MarkdownFile.MARKDOWN_DEF_EXT) || sres.ToLower().EndsWith(MarkdownFile.HTML_EXT))   //FPF
                    {
                        try
                        {
                            string fpfPath = ctx.Server.MapPath(sres);    //The File-Processing Field path
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
                    else if (sres.StartsWith(FRONT_MATTER_SOURCES_PREFIX))  //Custom FM Source
                    {
                        //Get the name of the source and it's params splitting the string (the first element would be the name of the surce, and the rest, the parameters, if any
                        string[] srcelements = sres.Substring(FRONT_MATTER_SOURCES_PREFIX.Length).Trim().Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                        if (srcelements.Length > 0)
                            return FieldValuesHelper.GetFieldValueFromFMSource(srcelements[0], srcelements.Skip(1).ToArray());
                    }
                    break;
            }

            return res;
        }
    }
}