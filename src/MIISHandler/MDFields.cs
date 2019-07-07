using System;
using System.Collections.Generic;
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
        private readonly MIISFile mdProxy;
        private readonly HttpContext ctx;

        //This dictionary prevents custom field values to be retrieved more than once, at the cost of taking up a little bit more memory
        private IDictionary<string, object> InternalFileFieldCache = new Dictionary<string, object>();

        //Constructor
        public MDFieldsResolver(MarkdownFile mdFile, HttpContext context)
        {
            md = mdFile;
            mdProxy = new MIISFile(md);
            ctx = context;
        }

        //Retrieves the value for the specified field or returns an empty string if it doesn't exist
        protected override object GetValue(string name)
        {
            object res = "";    //Default value (empty string)
            switch (name.ToLower())
            {
                //Check well Known fields first
                case INTERNAL_REFERENCE_TO_CURRENT_FILE:
                    //This is intended to be used internally only, from custom tags or front-matter custom sources
                    res = mdProxy;
                    break;
                case "title":
                    res = md.Title;
                    break;
                case "filename":
					res = md.FileName;
					break;
                case "date":
                    res = md.Date;
                    break;
                case "datecreated":
                    res = md.DateCreated;
					break;
                case "datemodified":
                    res = md.DateLastModified;
					break;
                case "isauthenticated":
                    res = ctx.User.Identity.IsAuthenticated;
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
                    res = DateTime.Now;
					break;
                case "time":
                    res = DateTime.Now;
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
                    //Check if the custom field has already been retrieved before
                    bool isCached = InternalFileFieldCache.TryGetValue(name, out res);  //If it's cached the value will be saved to res
                    if (!isCached)  //If it's not cached (has not been retrieved before) then retrieve it
                    {
                        res = string.Empty;   //Default value
                        /*
                         * There are 4 types of fields:
                         * - Value fields: {{name}} -> Get a value from the Front-Matter or from web.config -> Simply replace them (default assumption)
                         * - File processing fields (FPF), ending in .md or .mdh. ej: myfile.md -> The file is read and it's contents transformed into HTML take the place of the placeholder
                         *   Useful for menus, and other independet parts in custom templates and parts of the same page.
                         * - Custom Dinamic Field Sources, that start with !! and use a custom class to populate the field with an object. Ej: !!customSource param1 param2
                         * - Querystring or Form fields, retrieved from the current request
                        */

                        //Simple value fields
                        string rawValue = FieldValuesHelper.GetFieldValue(name, md);

                        //First, Custom Dinamic Field Sources that provide values from external assemblies
                        if (rawValue.StartsWith(FRONT_MATTER_SOURCES_PREFIX))
                        {
                            //Get the name of the source and it's params splitting the string (the first element would be the name of the surce, and the rest, the parameters, if any
                            string[] srcelements = rawValue.Substring(FRONT_MATTER_SOURCES_PREFIX.Length).Trim().Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                            if (srcelements.Length > 0)
                                res = FieldValuesHelper.GetFieldValueFromFMSource(srcelements[0], mdProxy, srcelements.Skip(1).ToArray());
                        }
                        //Second, File Processing Fields, thar inject the content of .md or .mdh files without proceesing their inner fields (for that you need to use the inject custom tag)
                        else if (rawValue.ToLower().EndsWith(MarkdownFile.MARKDOWN_DEF_EXT) || rawValue.ToLower().EndsWith(MarkdownFile.HTML_EXT))
                        {
                            try
                            {
                                string fpfPath = ctx.Server.MapPath(rawValue);    //The File-Processing Field path
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
                        //Third, try to determine from the querystring or the form values
                        else if (ctx.Request.QueryString[name] != null || ctx.Request.Form[name] != null)
                        {
                            res = ctx.Request.Params[name];
                        }
                        //Finally, if it's not a custom source, or a FPF or a request parameter, then is a normal raw value
                        else
                        {
                            res = rawValue;
                        }
                        
                        //Cache the retrieved value
                        InternalFileFieldCache[name] = res;
                    }
                    //Get out of the switch
                    break;
            }
            //Return retrieved value
            return res;
        }
    }
}