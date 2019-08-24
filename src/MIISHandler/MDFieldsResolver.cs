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

        private readonly MarkdownFile _md;
        private readonly MIISFile _mdProxy;
        private readonly HttpContext _ctx;

        //This dictionary prevents custom field values to be retrieved more than once, at the cost of taking up a little bit more memory
        private readonly IDictionary<string, object> InternalFileFieldCache = new Dictionary<string, object>();

        //Constructor
        public MDFieldsResolver(MarkdownFile mdFile, HttpContext context)
        {
            _md = mdFile;
            _mdProxy = new MIISFile(_md);
            _ctx = context;
        }

        public MDFieldsResolver(MarkdownFile mdFile):this(mdFile, HttpContext.Current)
        {
            //Just to ease the initialization from extensions
        }

        //Retrieves the value for the specified field or returns an empty string if it doesn't exist
        protected override object GetValue(string name)
        {
            object res = "";    //Default value (empty string)
            switch (name.ToLowerInvariant())
            {
                //Check well Known fields first
                case INTERNAL_REFERENCE_TO_CURRENT_FILE:
                    //This is intended to be used internally only, from custom tags or front-matter custom sources
                    res = _mdProxy;
                    break;
                case "content": //The final HTML content, WITHOUT the template and WITH liquid tags processed
                    res = _md.RawFinalHtml; //This is needed to avoid that the Markdown conversion messes up with the liquid tags (loops, conditionals...)
                    break;
                case "title":
                    res = _md.Title;
                    break;
                case "excerpt":
                case "description":
                case "summary":
                    res = _md.Excerpt;
                    break;
                case "filename":
					res = _md.FileName;
					break;
                case "filenamenoext":
                    res = _md.FileNameNoExt;
                    break;
                case "fileext":
                    res = _md.FileExt;
                    break;
                case "dir":
                    res = _mdProxy.Dir;
                    break;
                case "date":
                    res = _md.Date;
                    break;
                case "datecreated":
                    res = _md.DateCreated;
					break;
                case "datemodified":
                    res = _md.DateLastModified;
					break;
                case "isauthenticated":
                    res = _ctx.User.Identity.IsAuthenticated;
					break;
                case "authtype":
                    res = _ctx.User.Identity.AuthenticationType;
					break;
                case "username":
                    res = _ctx.User.Identity.Name;
					break;
                case "domain":
                    res = _ctx.Request.Url.Authority;
					break;
                case "baseurl":
                    res = $"{_ctx.Request.Url.Scheme}{System.Uri.SchemeDelimiter}{_ctx.Request.Url.Authority}";
					break;
                case "now":
                case "today":
                    res = DateTime.Now;
					break;
                case "time":
                    res = DateTime.Now.ToString("hh:mm:ss tt");
					break;
                case "url":
                    res = _ctx.Request.Url.AbsolutePath;
					break;
                case "urlnoext":
                    //Files processed by MIIS always have extension on disk
                    res = _ctx.Request.Path.Remove(_ctx.Request.Path.LastIndexOf("."));
					break;
                case "templatename":
                    res = _md.TemplateName;
                    break;
                case "layout":
                    res = _md.Layout;
                    break;
                //Custom fields
                default:
                    //Check if the custom field has already been retrieved before
                    bool isCached = InternalFileFieldCache.TryGetValue(name, out res);  //If it's cached the value will be saved to res
                    if (!isCached)  //If it's not cached (has not been retrieved before) then retrieve it
                    {
                        res = string.Empty;   //Default value

                        /*
                         * There are 4 types of custom fields:
                         * - Value fields: {{name}} -> Get a value from the Front-Matter or from web.config -> Simply replace them (default assumption)
                         * - File processing fields (FPF), {{name}} whose value ends in .md or .mdh. ej: myfile.md -> if available the file is read and it's contents transformed into HTML take the place of the placeholder
                         *   Useful for menus, and other independent parts in custom templates and parts of the same page.
                         * - Custom Dinamic Field Sources, {{name}} value that start with !! and use a custom class to populate the field with an object. Ej: !!customSource param1 param2
                         * - Querystring or Form fields, retrieved from the current request
                        */

                        //Try to get a typed value for the field
                        var typedValue = FieldValuesHelper.GetFieldObject(name, _md, null);
                        //If we get a string already, then process it
                        if (typedValue is string)
                        {
                            //If it's a string, process it for special fields

                            //////////////////////////////
                            //Simple value fields (default value if present)
                            //////////////////////////////

                            string stringValue = typedValue.ToString();

                            //////////////////////////////
                            //First, Custom Dinamic Field Sources that provide values from external assemblies
                            //////////////////////////////
                            if (stringValue.StartsWith(FRONT_MATTER_SOURCES_PREFIX))
                            {
                                //Get the name of the source and it's params splitting the string (the first element would be the name of the source, and the rest, the parameters, if any
                                string[] srcelements = stringValue.Substring(FRONT_MATTER_SOURCES_PREFIX.Length).Trim().Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                                if (srcelements.Length > 0)
                                    res = FieldValuesHelper.GetFieldValueFromFMSource(srcelements[0], _mdProxy, srcelements.Skip(1).ToArray());
                            }
                            //////////////////////////////
                            //Second, File Processing Fields, thar inject the content of .md or .mdh files without processing their inner fields (for that, you need to use the 'injectfile' custom tag, if installed)
                            //////////////////////////////
                            else if (stringValue.ToLowerInvariant().EndsWith(MarkdownFile.MARKDOWN_DEF_EXT) || stringValue.ToLowerInvariant().EndsWith(MarkdownFile.HTML_EXT))
                            {
                                try
                                {
                                    string fpfPath = _ctx.Server.MapPath(stringValue);    //The File-Processing Field path
                                    MarkdownFile mdFld = new MarkdownFile(fpfPath);
                                    res = mdFld.RawFinalHtml; //Use the raw HTML (WITHOUT the template (obviously) and WITH the liquid tags processed in its own context)

                                    //Add the processed file to the dependencies of the currently processed content file, 
                                    //so that the file is invalidated when the FPF changes (if caching is enabled)
                                    //The FPF is already cached too if caching is enabled
                                    _md.Dependencies.Add(fpfPath);
                                }
                                catch (System.Security.SecurityException)
                                {
                                    res = String.Format("Can't access file for {0}", TemplatingHelper.PLACEHOLDER_PREFIX + name + TemplatingHelper.PLACEHOLDER_SUFFIX);
                                }
                                catch (System.IO.FileNotFoundException)
                                {
                                    res = String.Format("File not found for {0}", TemplatingHelper.PLACEHOLDER_PREFIX + name + TemplatingHelper.PLACEHOLDER_SUFFIX);
                                }
                                catch (Exception ex)
                                {
                                    //This should only happen while testing, never in production, so I send the exception's message
                                    res = String.Format("Error loading {0}: {1}", TemplatingHelper.PLACEHOLDER_PREFIX + name + TemplatingHelper.PLACEHOLDER_SUFFIX, ex.Message);
                                }
                            }
                            //////////////////////////////
                            //Finally, if it's not a custom source, or a FPF, then is a normal raw string value
                            //////////////////////////////
                            else
                            {
                                res = stringValue;
                            }
                        }
                        else
                        {
                            //If we already got a typed value, then return it by default
                            res = typedValue;

                            //Check if it's null. If it is null, means it hasn't been found in he fields for the file or app, 
                            //so maybe the last chance is that it's a request param
                            if (typedValue == null)
                            {
                                //////////////////////////////
                                //Try to determine the param value from the querystring or the form values
                                //////////////////////////////
                                if (!string.IsNullOrWhiteSpace(_ctx.Request.Params[name]))
                                {
                                    var paramVal = _ctx.Request.Params[name]; //Result (checks qs, form data, cookies and sv, in that order)
                                    if (!string.IsNullOrWhiteSpace(paramVal))
                                    {
                                        res = paramVal; //A value has been found
                                        _md.CachingEnabled = false; //Disable caching if a param is to be used
                                    }
                                }
                            }
                        }

                        //Cache the retrieved value
                        InternalFileFieldCache[name] = res;
                    }
                    //Get out of the switch construction
                    break;
            }
            //Return retrieved value
            return res;
        }
    }
}