using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;
using System.IO;
using System.Text.RegularExpressions;
using Markdig;
using IISHelpers;
using IISHelpers.YAML;

namespace MIISHandler
{

    /// <summary>
    /// Loads and processes a markdown file
    /// </summary>
    public class MarkdownFile
    {
        public static string[] _CachingQueryStringFields = new string[] { };

        public const string MARKDOWN_DEF_EXT = ".md";   //Default extension for markdown files
        public const string HTML_EXT = ".mdh";  //File extension for HTML content
        //It allows more than 3 dashed to be used to delimit the Front-Matter (the YAML spec requires exactly 3 dashes, but I like to allow more freedom on this, so 3 or more in a line are allowed)
        //It takes into account the different EOL for Windows (\r\n), Mac (\r) or UNIX (\n)
        private static readonly Regex FRONT_MATTER_RE = new Regex(@"^-{3,}(.*?)-{3,}\s*?(\r\n|\r|\n|$)", RegexOptions.Singleline);

        #region private fields
        private string _rawContent = string.Empty;
        private string _processedContent = string.Empty;
        private string _rawHtmlContent = string.Empty;
        private string _rawFinalHtml = string.Empty;
        private string _finalHtml;
        private bool _processSubFiles = true;   //Used by sub-files inserted with a File Processing Field (FPF) to prevent the processing of subfiles in that case
        private MDFieldsResolver _resolver = null;    //Internal reference to a MDFields resolver to be reused while the MarkdownFile is used
        private string _title;
        private string _filename;
        private DateTime _dateCreated;
        private DateTime _dateLastModified;
        private DateTime _date;
        private string _layout;
        private SimpleYAMLParser _FrontMatter;
        private bool? _CachingEnabled = null;    //Should be default to allow for the expression shortcircuit in the CachingEnabled property
        private double _NumSecondsCacheIsValid = 0;
        #endregion

        #region Constructors
        //Reads and process the file. 
        //IMPORTANT: Expects the PHYSICAL path to the file.
        //Possibly generates errors that must be handled in the call-stack
        public MarkdownFile(string mdFilePath)
        {
            this.FilePath = mdFilePath;
            //Initialize the file dependencies
            this.Dependencies = new List<string>
            {
                this.FilePath   //Add current file as cache dependency (the render process will add the fragments and other files if needed)
            };
        }

        public MarkdownFile(string mdFilePath, bool processSubFiles) : this(mdFilePath)
        {
            _processSubFiles = processSubFiles;
        }

        #endregion

        #region Properties
        //Complex properties
        public string FilePath { get; private set; } //The full path to the file

        #region Content related properties
        /// <summary>
        /// The raw file content, read from disk, without any further processing, without the Front-Matter
        /// </summary>
        public string RawContent
        {
            get
            {
                EnsureContentAndFrontMatter();
                return _rawContent;
            }
        }

        /// <summary>
        /// The raw file content (Markdown or HTML) WITHOUT the template and WITH liquid tags processed
        /// </summary>
        public string ProcessedContent
        {
            get
            {
                //If the content has not been processed yet...
                if (string.IsNullOrEmpty(_processedContent))
                {
                    //Process liquid tags
                    _processedContent = Renderer.RenderLiquidTags(this.RawContent, this);
                }

                //then return the raw content
                return  _processedContent;
            }
        }

        /// <summary>
        /// The file content transformed into HTML from the raw content, WITHOUT the template and WITHOUT the liquid tags procesed
        /// </summary>
        public string RawHtmlContent
        {
            get
            {
                if (string.IsNullOrEmpty(_rawHtmlContent))
                {
                    //Check if its a pure HTML file (.mdh extension)
                    if (this.FileExt == HTML_EXT)  //It's HTML
                    {
                        //No transformation required --> It's already an HTML file
                        _rawHtmlContent = this.RawContent;
                    }
                    else  //Is markdown: transform into HTML
                    {
                        //Convert markdown to HTML
                        _rawHtmlContent = Renderer.ConvertMarkdown2Html(this.RawContent, this.UseEmoji, this.EnabledMDExtensions);
                    }
                }

                return _rawHtmlContent;
            }
        }

        /// <summary>
        /// The HTML content of the file, WITHOUT the template (except in components), and WITH the liquid tags processed
        /// </summary>
        public string RawFinalHtml
        {
            get
            {
                //In the normal case, pprocess the main content
                if (string.IsNullOrEmpty(_rawFinalHtml))
                {
                    //Try to read it from the cache
                    _rawFinalHtml = GetRawHtmlFromCache();
                    if (string.IsNullOrEmpty(_rawFinalHtml))    //If it's not in the cache, process the raw content
                    {
                        //Check if its a pure HTML file (.mdh extension)
                        if (this.FileExt == HTML_EXT)  //It's HTML
                        {
                            //No transformation required --> It's already an HTML file
                            _rawFinalHtml = this.ProcessedContent;
                        }
                        else  //Is markdown: transform into HTML
                        {
                            //Convert markdown to HTML
                            _rawFinalHtml = Renderer.ConvertMarkdown2Html(this.ProcessedContent, this.UseEmoji, this.EnabledMDExtensions);
                            AddRawHtmlToCache();
                        }
                    }
                }
                return _rawFinalHtml;
            }
        }

        /// <summary>
        /// The final HTML generated from the markdown content and the current template, so WITH the template applied and WITH the liquid tags processed
        /// </summary>
        public string FinalHtml
        {
            get
            {
                if (string.IsNullOrEmpty(_finalHtml))    //If it's not processed yet
                {
                    //Try to read from cache
                    _finalHtml = GetFinalHtmlFromCache();
                    if (string.IsNullOrEmpty(_finalHtml)) //If it's not in the cache, process the file
                    {
                        _finalHtml = Renderer.RenderMarkdownFile(this);
                        AddFinalHtmlToCache();  //Add to cache (if enabled)
                    }
                }
                return _finalHtml;
            }
        }

        /// <summary>
        /// Renders the full HTML (with layout) or just the raw HTML depending on the current file being a component or not
        /// </summary>
        public string ComponentHtml
        {
            get
            {
                //Check if the file is a component, in which case, render the full HTML
                if (this.IsComponent)
                    return this.FinalHtml;
                else
                    return this.RawFinalHtml;
            }
        }

        public MDFieldsResolver FieldsResolver
        {
            get
            {
                if (_resolver == null)
                    _resolver = new MDFieldsResolver(this);

                return _resolver;
            }
        }

        #endregion

        #region "Plumbing" properties
        //Current Template name
        public string TemplateName
        {
            get
            {
                return FieldValuesHelper.GetFieldValue("TemplateName", this);
            }
        }

        //Current layout file name
        public string Layout
        {
            get
            {
                if (string.IsNullOrEmpty(_layout))
                    _layout = FieldValuesHelper.GetFieldValue("Layout", this);
                return _layout;
            }
            set
            {
                _layout = value;
            }
        }

        //Checks if the file is marked as a component in the front-matter
        public bool IsComponent
        {
            get
            {
                return TypesHelper.IsTruthy(FieldValuesHelper.GetFieldValueFromFM("IsComponent", this, "false"));
            }
        }

        //Used internally for rendering the file as a component (component property)
        //Gets the value of the field "component" when rendering the file as a partial element of the content (File processing fields or with InserFile)
        internal string ComponentLayout
        {
            get
            {
                return FieldValuesHelper.GetFieldValueFromFM("component", this);
            }
        }

        //This is only used for the special case of HTML being rendered inside Markdown
        public bool MustProcessSubFiles
        {
            get
            {
                return _processSubFiles;
            }
        }

        //The object encapsulating access to Front Matter properties
        public SimpleYAMLParser FrontMatter
        {
            get
            {
                if (_FrontMatter == null)
                {
                    //ensureContentAndFrontMatter();
                    ProcessFrontMatter();
                }

                return _FrontMatter;
            }
        }

        #endregion

        //The title of the file (first available H1 header or the file name)
        public string Title
        {
            get
            {
                if (string.IsNullOrEmpty(_title))
                    _title = FieldValuesHelper.GetFieldValue("title", this, this.FileNameNoExt);    //Use the file name, with no extension, as the default title

                return _title;
            }
        }

        /// <summary>
        /// Excerpt for the page if present in the Front-Matter. It looks for the fields: excerpt, description & summary, in that order of precedence
        /// If none is found, then returns an empty string
        /// </summary>
        public string Excerpt
        {
            get
            {
                string res = FieldValuesHelper.GetFieldValue("excerpt", this, string.Empty);
                if (res == string.Empty)
                {
                    res = FieldValuesHelper.GetFieldValue("description", this, string.Empty);
                    if (res == string.Empty)
                    {
                        res = FieldValuesHelper.GetFieldValue("summary", this, string.Empty);
                    }
                }
                return res;
            }
        }

        

        //Basic properties directly gotten from the file info

        //The file name
        public string FileName {
            get {
                if (!string.IsNullOrEmpty(_filename))
                    return _filename;

                FileInfo fi = new FileInfo(this.FilePath);
                _filename = fi.Name;
                return _filename;
            }
        }
        
        //THe file name without the extension
        public string FileNameNoExt
        {
            get
            {
                return this.FileName.Substring(0, this.FileName.Length - this.FileExt.Length);
            }
        }

        //The file extension (with dot)
        public string FileExt
        {
            get {
                return Path.GetExtension(this.FileName).ToLowerInvariant();
            }
        }

        //Date when the file was created
        public DateTime DateCreated {
            get {
                if (_dateCreated != default)
                    return _dateCreated;

                FileInfo fi = new FileInfo(this.FilePath);
                _dateCreated = fi.CreationTime;
                return _dateCreated;
            }
        }

        //Date when the file was last modified
        public DateTime DateLastModified {
            get
            {
                if (_dateLastModified != default)
                    return _dateLastModified;

                FileInfo fi = new FileInfo(this.FilePath);
                _dateLastModified = fi.LastWriteTime;
                return _dateLastModified;
            }
        }

        //The date indicated in the "date" filed of the Front-Matter, used to get the date when the file should get published
        //If it's present this date will be take into account before making a file published
        public DateTime Date
        {
            get
            {
                if (_date != default)
                    return _date;

                _date = FieldValuesHelper.GetFieldObject("date", this, this.DateCreated);
                return _date;
            }
        }

        //The file paths of files the current file depends on, including itself (current file + fragments)
        internal List<string> Dependencies { get; private set; }

        //A list of possible Markdig extensions to enable apart from the advanced ones that are enabled by default
        //THe name differs from the name of the parameter (Enabled vs Enable, because of the point of view in each case)
        public string EnabledMDExtensions
        {
            get
            {
                return FieldValuesHelper.GetFieldValue("EnableMDExtensions", this);
            }
        }

        public bool UseEmoji
        {
            get
            {
                //Check if we must generate emojis
                return TypesHelper.IsTruthy(FieldValuesHelper.GetFieldValue("UseEmoji", this, "1"));
            }
        }

        //If the file is published or is explicitly forbidden by the author using the "Published" field
        public bool IsPublished
        {
            get
            {
                //Check if the File is published with the "Published" field
                string isPublished = FieldValuesHelper.GetFieldValue("Published", this, "1").ToLowerInvariant();
                //For the sake of security, if it's not explicitly a "falsy" value, then is assumed true
                //And the file date, if specified, should be greater or equal than the current date
                return !TypesHelper.IsFalsy(isPublished) && DateTime.Now >= this.Date;
            }
        }

        //Determines a special status code to return for the file (default is 200, OK)
        //Valid status codes: https://docs.microsoft.com/en-us/windows/desktop/WinHttp/http-status-codes
        internal int HttpStatusCode
        {
            get
            {
                //Check if the File has a status code that is not a 200 (default OK status code)
                string statusCode = FieldValuesHelper.GetFieldValueFromFM("HttpStatusCode", this, "200").ToLowerInvariant();
                bool isInt = int.TryParse(statusCode, out int nStatus);
                if (!isInt) nStatus = 200;
                return Math.Abs(nStatus);
            }
        }

        //Determines if the page has an special MIME type specified
        internal string MimeType
        {
            get
            {
                //Check if the File has an special MIME type set in the Front Matter
                string mimeType = FieldValuesHelper.GetFieldValueFromFM("MIME", this, "text/html").ToLowerInvariant();
                //It should contain a slash, but not in the first character
                 return mimeType.IndexOf("/") > 1 ? mimeType:  "text/html";
            }
        }

        #endregion

        #region caching

        //If false, then caching is not applied even if it's enabled in the settings.
        //This is used by custom tags and fields to keep the document fresh
        //This is only checked to save the file in the cache, but not to retrieve it
        //It works OK, because if you change the setting (in web.config or in the file) 
        //the cache gets invalidated (the app domain restarts or the file is a cache dependency, respectively)
        internal bool CachingEnabled
        {
            get
            {
                //Returns true if caching is enabled in the file or global settings, and if is not disabled by a custom tag or param
                if (_CachingEnabled == null)
                {
                    _CachingEnabled = (
                        TypesHelper.IsTruthy(FieldValuesHelper.GetFieldValue("Caching", this, "0")) ||
                        TypesHelper.IsTruthy(FieldValuesHelper.GetFieldValue("UseMDCaching", this, "0") //Compatibility with 2.x
                        )
                    );
                }
                return _CachingEnabled.Value;
            }
            set
            {
                _CachingEnabled = value;
            }
        }

        //Set by custom tags or params. 0 means no time based expiration. Maximum value 1 day (24 hours)
        internal double NumSecondsCacheIsValid
        {
            get
            {
                return _NumSecondsCacheIsValid;
            }
            set
            {
                _NumSecondsCacheIsValid = value;
                if (value <= 0) _NumSecondsCacheIsValid = 0;
                //if (value > 86400) _NumSecondsCacheIsValid = 86400;    //24 hours in seconds
            }
        }

        //Adds one or more field names to the global list of fields that will be used for caching variants of the page
        //This is only called when extensions that implement IQueryStringDependent are registered in the system
        internal static void AddCachingQueryStringFields(string[] flds)
        {
            Regex validnames = new Regex(@"^[a-z\d\-_\.]+$"); //per https://tools.ietf.org/html/rfc3986
            //Save only valid names, in lowercase (case insensitive) and in alphabetical order
            _CachingQueryStringFields = _CachingQueryStringFields
                .Union<string>(flds
                        .Select(s => s.Trim().ToLowerInvariant())
                        .Where(s => validnames.IsMatch(s))
                    )
                .OrderBy(s => s).ToArray();
        }

        //Gets the unique query string to be used as a suffix for caching the contents of the page
        internal string GetQueryStringCachingSuffix()
        {
            if (string.IsNullOrWhiteSpace(HttpContext.Current.Request.Url.Query))
                return string.Empty;

            var qsFlds = HttpContext.Current.Request.QueryString;
            //Get all fields that have values in the current qs joined in the form field=value&field2=value2...
            string res = string.Join("&",
                            (from f in _CachingQueryStringFields
                            where !string.IsNullOrWhiteSpace(qsFlds[f])
                            select string.Format("{0}={1}", f, qsFlds[f]))
                            .ToArray<string>()
                        );
            return string.IsNullOrEmpty(res) ? res : "?" + res; ;
        }

        //Adds the specified value to the cache with the specified key, if it's enabled
        private void AddToCache(string CacheID, string content)
        {
            if (this.CachingEnabled)
            {
                if (this.NumSecondsCacheIsValid > 0)
                {
                    //Cache invalidation when files change or after a certain time
                    HttpRuntime.Cache.Insert(CacheID, content, new CacheDependency(this.Dependencies.ToArray()),
                        DateTime.UtcNow.AddSeconds(this.NumSecondsCacheIsValid), Cache.NoSlidingExpiration);
                }
                else
                {
                    //Cache invalidation just when files change
                    HttpRuntime.Cache.Insert(CacheID, content, new CacheDependency(this.Dependencies.ToArray())); //Add result to cache with dependency on the file
                }
            }
        }

        //Adds the specified content to the FinalHtml cache (if enabled) using the correct id 
        //depending on if the query string should be used or not
        private void AddFinalHtmlToCache()
        {
            if (!CachingEnabled) return;

            AddToCache(CachingIdHtml + GetQueryStringCachingSuffix(), _finalHtml);
        }

        //Adds the specified content to the RawHtml cache (if enabled) using the correct id 
        //depending on if the query string should be used or not
        private void AddRawHtmlToCache()
        {
            if (!CachingEnabled) return;

            AddToCache(CachingIdRawHtml + GetQueryStringCachingSuffix(), _rawFinalHtml);
        }

        //Gets value from the cache if available
        private string GetFromCache(string CacheID)
        {
            return HttpRuntime.Cache[CacheID] as string;
            //NOTE to self: doesn't check if caching is enabled because I want the setting to be available in the front-matter too
            //and, at the point this method is called, the FM is not usually available yet, and I want to avoid reading the file 
            //on each request (that's the main purpose of caching here).
            //The original version of MIIS only allowed this setting to be global, in the web.config for the same reason.
        }

        //Gets the rendered HTML from cache, if available, trying to get it with the query string
        //or just from the default ID (no query string)
        private string GetFinalHtmlFromCache()
        {
            return GetFromCache(this.CachingIdHtml + GetQueryStringCachingSuffix());
        }

        //Gets the rendered HTML from cache, if available, trying to get it with the query string
        //or just from the default ID (no query string)
        private string GetRawHtmlFromCache()
        {
            return GetFromCache(this.CachingIdRawHtml);
        }

        //Returns the internal identifier to be used as the key for the FinalHtml caching entry of this document
        private string CachingIdHtml
        {
            get
            {
                return this.FilePath + "_HTML";
            }
        }

        //Returns the internal identifier to be used as the key for the RawHtml caching entry of this document
        private string CachingIdRawHtml
        {
            get
            {
                return this.FilePath + "_RawHTML";
            }
        }

        //Returns the internal identifier to be used as the key for the caching entry of this document's Front Matter
        private string CachingIDFrontMatter
        {
            get
            {
                return this.FilePath + "_FM";
            }
        }

        #endregion

        #region Aux methods
        //Ensures that the content of the file is loaded from disk and the Front Matter processed & removed from it
        private void EnsureContentAndFrontMatter()
        {
            EnsureContent();
            ProcessFrontMatter();   //Make sure the FM is processed
            RemoveFrontMatter();    //This is a separate step because FM can be cached and it's only dependent of the current file
        }

        //Ensures that the content of the file is loaded from disk
        private void EnsureContent()
        {
            if (string.IsNullOrEmpty(_rawContent))
            {
                _rawContent = IOHelper.ReadTextFromFile(this.FilePath);
                //Remove {{Content}} tag if present, since this tag is exclusive of layout templates and can't be included in raw content
                if (TemplatingHelper.IsPlaceHolderPresent(_rawContent, "content"))
                {
                    _rawContent = TemplatingHelper.ReplacePlaceHolder(_rawContent, "content", string.Empty);
                }
            }
        }

        //Extracts Front-Matter from current file
        private void ProcessFrontMatter()
        {
            if (_FrontMatter != null)
                    return;

            string strFM = string.Empty;

            strFM = GetFromCache(this.CachingIDFrontMatter);
            if (!string.IsNullOrEmpty(strFM)) //If it in the cache, just use it
            {
                _FrontMatter = new SimpleYAMLParser(strFM);
                return; //Ready!
            }
            else
            {
                //Assign a default empty FrontMatter (if an empty string was used, contents would be read every time for all the files without a Front Matter)
                strFM = "---\r\n---";
            }

            //If cache is not enabled or the FM is not currently cached, read it from the file content
            //Default value (empty FM, but no empty string), prevents the Content property from processing Front-Matter twice if it's not read yet
            _FrontMatter = new SimpleYAMLParser(strFM);

            //Extract and remove YAML Front Matter
            EnsureContent();
            Match fm = FRONT_MATTER_RE.Match(this._rawContent);
            if (fm.Length > 0) //If there's front matter available
            {
                strFM = fm.Groups[0].Value;
                //Save front matter text
                _FrontMatter = new SimpleYAMLParser(strFM);
            }

            //Cache the final FM content (if caching is enabled)
            AddToCache(this.CachingIDFrontMatter, strFM);
        }

        //Removes the front matter, if any, from the actual content of the file
        //and removes the extra empty lines at the beginning and the end
        private void RemoveFrontMatter()
        {
            _rawContent = FRONT_MATTER_RE.Replace(_rawContent, "").Trim('\r', '\n');
        }
        #endregion
    }
}
