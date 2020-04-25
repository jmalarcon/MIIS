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
        private bool _isPersistedToCache = false;
        #endregion

        #region Internal classes, structs and enums
        //Nested private helper class to keep a unique cache for the file
        /*
         * Note: in fact, each component has different dependencies:
         * - RawHtml: current file, it's fragments and inserted files, extension dependencies...
         * - FinalHtml: the previous ones and the dependencies for the template (files, includes...)
         * - FrontMatter: just his file
         * However, for the sake of simplicity I use just one cache for everything and therefore, 
         * for example, the FM or RawHTML would be invalidated if a template file changes.
         * It doesn't matter much bacuase, normally, when this happens most of the times the file need to be read again from disk
        */
        private class MarkdownFileCacheItem
        {
            public string RawHTML;  //Raw final HTML (without template and with liquid tags processed)
            public string FinalHTML; //Final HTML (with template and with liquid tags processed)
            public string FrontMatter;  //File's Front Matter
        }

        //THe type of item to add to cache
        private enum CachedContentType
        {
            FinalHtml,
            RawHtml,
            FrontMatter
        }
        #endregion

        #region Constructors
        //Reads and process the file. 
        //IMPORTANT: Expects the PHYSICAL path to the file.
        //Possibly generates errors that must be handled in the call-stack
        public MarkdownFile(string mdFilePath)
        {
            this.FilePath = mdFilePath;
            //Initializae cache item
            this.CachedContentItem = HttpRuntime.Cache[this.CachingId] as MarkdownFileCacheItem;
            if (this.CachedContentItem == null) this.CachedContentItem = new MarkdownFileCacheItem();

            //Initialize the file dependencies
            this.Dependencies = new List<string>
            {
                this.FilePath   //Add current file as cache dependency (the render process will add the fragments and other files if needed)
            };

            //No special dependencies by default (normally this is not used)
            this.SpecialDependencies = null;
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
        /// It's used for FPFs
        /// </summary>
        public string RawFinalHtml
        {
            get
            {
                //In the normal case, pprocess the main content
                if (string.IsNullOrEmpty(_rawFinalHtml))
                {
                    //Try to read it from the cache
                    _rawFinalHtml = GetFromCache(CachedContentType.RawHtml);
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
                        }
                        AddToCache(_rawFinalHtml, CachedContentType.RawHtml);
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
                    _finalHtml = GetFromCache(CachedContentType.FinalHtml);
                    if (string.IsNullOrEmpty(_finalHtml)) //If it's not in the cache, process the file
                    {
                        _finalHtml = Renderer.RenderMarkdownFile(this);
                        AddToCache(_finalHtml, CachedContentType.FinalHtml);    //Add to cache (if enabled)
                        PersistToCache();   //Make sure the final cached content gets saved to cache (if caching is enabled)
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
                string html;
                //Check if the file is a component, in which case, render the full HTML
                if (this.IsComponent)
                { 
                    html = this.FinalHtml;
                }
                else
                { 
                    html = this.RawFinalHtml;
                    this.PersistToCache();  //Since it's used as a component, persist the result to cache (above, FinalHtml does it automatically)
                }

                return html;
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

        //The file extension (with dot) in lower case
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

        //This can be used to cache the file using specialized dependencies and not only specific files or folders
        private List<CacheDependency> SpecialDependencies { get; set; }

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
        private string CachingId
        {
            get
            {
                return this.FilePath + GetQueryStringCachingSuffix();
            }
        }

        //Gets the cache item associated with this file
        //It's loaded at the constructor to recover possible cached values
        private MarkdownFileCacheItem CachedContentItem { get; set; }

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
                    _CachingEnabled = TypesHelper.IsTruthy(FieldValuesHelper.GetFieldValue("Caching", this, "0"));
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
        //It just takes into account registered query string fields declared by FM sources. Any other QS field is ignored
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
            return string.IsNullOrEmpty(res) ? "" : "?" + res;
        }

        /// <summary>
        /// Adds a file or folder dependency to the current file's dependencies
        /// </summary>
        /// <param name="path">The path of the file or forlder that is this file depends on</param>
        internal void AddFileDependency(string filePath)
        {
            //If is a valid file or folder and it's not already added to the dependencies, add it
            if (
                !this.Dependencies.Contains(filePath) &&
                (File.Exists(filePath) || Directory.Exists(filePath))
               )
                this.Dependencies.Add(filePath);
        }

        /// <summary>
        /// Adds a list of file or folder paths to the current file's dependencies
        /// </summary>
        /// <param name="filePaths">A List of paths</param>
        internal void AddFileDependencies(List<string> filePaths)
        {
            filePaths.ForEach(delegate(string filePath) {
                this.AddFileDependency(filePath);
            });
        }

        /// <summary>
        /// Adds an special cache dependency for the file (used by custom tags or FM sources)
        /// </summary>
        /// <param name="cd">The CacheDependency class to add as a dependency</param>
        internal void AddCacheDependency(CacheDependency cd)
        {
            if (this.SpecialDependencies == null)
            {
                this.SpecialDependencies = new List<CacheDependency> { 
                    cd
                };
            }
            else
            {
                //CHeck if it's already added
                bool alreadyPresent = this.SpecialDependencies.Any( 
                        item => item.GetUniqueID() == cd.GetUniqueID() 
                    );
                if (!alreadyPresent)
                    this.SpecialDependencies.Add(cd);
            }
        }

        //Aux var to keep a unique reference to special dependencies and not reusing the same reference more than one
        AggregateCacheDependency _aggregateCacheDependency = null;

        //Get the appropriate dependencies to be used for the file cache
        private CacheDependency GetCacheDependencies()
        {
            //Manage and reuse Cache dependencies

            //File dependencies
            CacheDependency deps = new CacheDependency(this.Dependencies.ToArray());

            //If there are any additional special dependency...
            if (this.SpecialDependencies != null)
            {
                //If a previous aggregate dependence exits, the special dependencies can't be added twice
                //so create one just the first time (reuse it)
                if (_aggregateCacheDependency == null)
                {
                    _aggregateCacheDependency = new AggregateCacheDependency();
                    _aggregateCacheDependency.Add(this.SpecialDependencies.ToArray());
                }
                //Combine files and folders with the special dependencies
                _aggregateCacheDependency.Add(deps);
                deps = _aggregateCacheDependency;
            }

            return deps;
        }

        //Adds the specified value to the cache with the specified key, if it's enabled
        private void AddToCache(string content, CachedContentType ccType)
        {
            //and assign property depending on content type
            switch (ccType)
            {
                case CachedContentType.FinalHtml:
                    this.CachedContentItem.FinalHTML = content;
                    break;
                case CachedContentType.RawHtml:
                    this.CachedContentItem.RawHTML = content;
                    break;
                case CachedContentType.FrontMatter:
                    this.CachedContentItem.FrontMatter = content;
                    break;
            }
        }

        //Gets value from the cache if available
        private string GetFromCache(CachedContentType ccType)
        {
            //NOTE to self: It doesn't check if caching is enabled because I want the setting to be available in the front-matter too
            //and, at the point this method is called, the FM is not usually available yet, and I want to avoid reading the file 
            //on each request (that's the main purpose of caching here).
            //The original version of MIIS only allowed this setting to be global, in the web.config for the same reason.

            switch (ccType)
            {
                case CachedContentType.FinalHtml:
                    return this.CachedContentItem.FinalHTML;
                case CachedContentType.RawHtml:
                    return this.CachedContentItem.RawHTML;
                case CachedContentType.FrontMatter:
                    return this.CachedContentItem.FrontMatter;
                default:
                    return "";
            }
        }

        //Persists the cached contents to Cache (in caching is enabled)
        internal void PersistToCache()
        {
            //Can only be persisted once!
            if (_isPersistedToCache) return;

            if (this.CachingEnabled)
            {
                CacheDependency deps = GetCacheDependencies();

                //Add to Cache
                if (this.NumSecondsCacheIsValid > 0)
                {
                    //Cache invalidation when files change or after a certain time
                    HttpRuntime.Cache.Insert(this.CachingId, this.CachedContentItem, deps,
                        DateTime.UtcNow.AddSeconds(this.NumSecondsCacheIsValid), Cache.NoSlidingExpiration);
                }
                else
                {
                    //Cache invalidation just when files change
                    HttpRuntime.Cache.Insert(this.CachingId, this.CachedContentItem, deps); //Add result to cache with dependency on the needed files and folders
                }
            }
            
            _isPersistedToCache = true;
        }

        #endregion

        #region Aux methods
        //Ensures that the content of the file is loaded from disk and the Front Matter processed & removed from it
        private void EnsureContentAndFrontMatter()
        {
            EnsureContent();
            ProcessFrontMatter();   //Make sure the FM is processed
            RemoveFrontMatter();    //This is a separate step because FM can be cached and we want to get rid of it in the file content even in that case
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

            //Non empty value, for caching
            string strFM = "---\r\n---";

            strFM = GetFromCache(CachedContentType.FrontMatter);
            if (!string.IsNullOrEmpty(strFM)) //If it in the cache, just use it
            {
                _FrontMatter = new SimpleYAMLParser(strFM);
                return; //Ready!
            }
            else
            {
                //If cache is not enabled or the FM is not currently cached, read it from the file content
                //Default value (empty FM, but no empty string), prevents the Content property from processing Front-Matter twice if it's not read yet
                _FrontMatter = new SimpleYAMLParser(strFM);
            }


            //Extract and remove YAML Front Matter
            EnsureContent();

            //If it's a .yml file, wrap the full content as Front-Matter (.yml files don't neeed to have the FM delimiters, but I wan't to support them)
            if (this.FileExt == ".yml" && !_rawContent.StartsWith("---\r\n"))
                _rawContent = "---\r\n" + _rawContent + "\r\n---";

            strFM = SimpleYAMLParser.GetFrontMatterFromContent(_rawContent);
            _FrontMatter = new SimpleYAMLParser(strFM);

            //Cache the final FM content (if caching is enabled)
            AddToCache(strFM, CachedContentType.FrontMatter);
        }

        //Removes the front matter, if any, from the actual content of the file
        //and removes the extra empty lines at the beginning and the end
        private void RemoveFrontMatter()
        {
            _rawContent = SimpleYAMLParser.RemoveFrontMatterFromContent(_rawContent);
        }
        #endregion
    }
}
