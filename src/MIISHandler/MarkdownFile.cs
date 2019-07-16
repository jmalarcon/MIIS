using System;
using System.Collections.Generic;
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

        public const string MARKDOWN_DEF_EXT = ".md";   //Default extension for markdown files
        public const string HTML_EXT = ".mdh";  //File extension for HTML contents
        private readonly Regex FRONT_MATTER_RE = new Regex(@"^-{3,}(.*?)-{3,}[\r\n]{1,2}", RegexOptions.Singleline);  //It allows more than 3 dashed to be used to delimit the Front-Matter (the YAML spec requires exactly 3 dashes, but I like to allow more freedom on this, so 3 or more in a line is allowed)

        #region private fields
        private string _content = "";
        private string _rawHtml;
        private string _html;
        private string _title;
        private string _filename;
        private DateTime _dateCreated;
        private DateTime _dateLastModified;
        private DateTime _date;
        private SimpleYAMLParser _FrontMatter;
        private bool _CachingEnabled = true;
        private double _NumSecondsCacheIsValid = 0;
        #endregion

        #region Constructor
        //Reads and process the file. 
        //IMPORTANT: Expects the PHYSICAL path to the file.
        //Possibly generates errors that must be handled in the call-stack
        //IMPORTANT: It doesn't need to cache the results because it's only directly used (and therefore read from disk)
        //in downloads (it could be cached for that but it's a not a frequent operation so it can be left that way instead of ocupping memory)
        //and in File-Processing fields, that use the rawHTML proprty and therefore this property too (and Markdown transformation) but in this case
        //the final result is cached and it's ony done once per file, so its effect it's neglictive and we save memory. 
        //This default behaviour could be changed in the future as needed.
        public MarkdownFile(string mdFilePath)
        {
            this.FilePath = mdFilePath;
            //Initialize the file dependencies
            this.Dependencies = new List<string>
            {
                this.FilePath   //Add current file as cache dependency (the render process will add the fragments and other files if needed)
            };
        }
        #endregion

        #region Properties
        //Complex properties
        public string FilePath { get; private set; } //The full path to the file
        
        //The raw file contents, read from disk
        public string Content
        {
            get
            {
                EnsureContentAndFrontMatter();
                return _content;
            }
        }

        //The raw HTML generated from the markdown contents
        public string RawHTML
        {
            get
            {
                if (string.IsNullOrEmpty(_rawHtml))
                {
                    //Check if its a pure HTML file (.mdh extension)
                    if (this.FileExt == HTML_EXT)  //It's HTML
                    {
                        //No transformation required --> It's an HTML file processed by the handler to mix with the current template
                        _rawHtml = this.Content;
                    }
                    else  //Is markdown: transform into HTML
                    {
                        //Configure markdown conversion
                        MarkdownPipelineBuilder mdPipe = new MarkdownPipelineBuilder().UseAdvancedExtensions();
                        //Check if we must generate emojis
                        if (FieldValuesHelper.GetFieldValue("UseEmoji", this, "1") != "0")
                        {
                            mdPipe = mdPipe.UseEmojiAndSmiley();
                        }
                        var pipeline = mdPipe.Build();
                        //Convert markdown to HTML
                        _rawHtml = Markdig.Markdown.ToHtml(this.Content, pipeline); //Converto to HTML
                    }
                }

                return _rawHtml;
            }
        }

        //The final HTML generated from the markdown contents and the current template
        public string HTML
        {
            get
            {
                if (string.IsNullOrEmpty(_html))    //If it's not processed yet
                {
                    //Try to read from cache
                    _html = GetFromCache(this.CachingIDHTML);
                    if (string.IsNullOrEmpty(_html)) //If it's not in the cache, process the file
                    {
                        _html = HTMLRenderer.RenderMarkdown(this);
                        AddToCache(this.CachingIDHTML, _html);  //Add to cache (if enabled)
                    }
                }
                return _html;
            }
        }

        //The title of the file (first available H1 header or the file name)
        public string Title
        {
            get
            {
                if (!string.IsNullOrEmpty(_title))
                    return _title;

                //If there's a title specified in the Front Matter, this is the one that prevails
                _title = this.FrontMatter["title"];

                if (string.IsNullOrEmpty(_title))   //If there's no title in the Front Matter
                {
                    if (this.FileExt == HTML_EXT)  //If it's just HTML
                    {
                        //Use the file name, with no extension, as the default title
                        _title = Path.GetFileNameWithoutExtension(this.FileName);
                    }
                    else
                    {
                        //Try to get the default title from the file the contents (find the first H1 if there's any)
                        //Quick and dirty with RegExp and only with "#".
                        Regex re = new Regex(@"^\s*?#\s(.*)$", RegexOptions.Multiline);
                        if (re.IsMatch(this.Content))
                            _title = re.Matches(this.Content)[0].Groups[1].Captures[0].Value;
                        else
                            _title = Path.GetFileNameWithoutExtension(this.FileName);
                    }
                }

                return _title;
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
        
        //The file extrension (with dot)
        public string FileExt
        {
            get {
                return Path.GetExtension(this.FileName);
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

                _date = TypesHelper.ParseUniversalSortableDateTimeString(this.FrontMatter["date"], this.DateCreated);
                return _date;
            }
        }

        //The file paths of files the current file depends on, including itself (current file + fragments)
        internal List<string> Dependencies { get; private set; }

        //If the file is published or is explicitly forbidden by the author using the "Published" field
        public bool IsPublished
        {
            get
            {
                //Check if the File is published with the "Published" field
                string isPublished = FieldValuesHelper.GetFieldValue("Published", this, "1").ToLower();
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
                string statusCode = FieldValuesHelper.GetFieldValueFromFM("HttpStatusCode", this, "200").ToLower();
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
                string mimeType = FieldValuesHelper.GetFieldValueFromFM("MIME", this, "text/html").ToLower();
                //It should contain a slash, but not in the first character
                 return mimeType.IndexOf("/") > 1 ? mimeType:  "text/html";
            }
        }


        #endregion

        #region caching

        //If false, then caching is not applied even if it's enabled in the settings.
        //This is used by custom tags and fields to keep the document fresh
        internal bool CachingEnabled
        {
            get
            {
                //Returns true if caching is enabled in the file or global settings, and if is not disabled by a custom tag or param
                return TypesHelper.IsTruthy(FieldValuesHelper.GetFieldValue("UseMDCaching", null, "1")) && _CachingEnabled;
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

        //Adds the specified value to the cache with the specified key, if it's enabled
        private void AddToCache(string CacheID, string contents)
        {
            if (this.CachingEnabled)
            {
                if (this.NumSecondsCacheIsValid > 0)
                {
                    //Cache invalidation when files change or after a certain time
                    HttpRuntime.Cache.Insert(CacheID, contents, new CacheDependency(this.Dependencies.ToArray()),
                        DateTime.UtcNow.AddSeconds(this.NumSecondsCacheIsValid), Cache.NoSlidingExpiration);
                }
                else
                {
                    //Cache invalidation just when files change
                    HttpRuntime.Cache.Insert(CacheID, contents, new CacheDependency(this.Dependencies.ToArray())); //Add result to cache with dependency on the file
                }
            }
        }

        //Gets value from the cache if enabled and available
        private string GetFromCache(string CacheID)
        {
            return this.CachingEnabled ? HttpRuntime.Cache[CacheID] as string : string.Empty;
        }

        //Returns the internal identifier to be used as the key for the caching entry of this document
        private string CachingIDHTML
        {
            get
            {
                return this.FilePath + "_HTML";
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
        //Ensures that the content of the file is loaded from disk
        private void EnsureContent()
        {
            if (string.IsNullOrEmpty(_content))
            {
                _content = IOHelper.ReadTextFromFile(this.FilePath);
            }
        }
        //Ensures that the content of the file is loaded from disk and the Front Matter processed & removed from it
        private void EnsureContentAndFrontMatter()
        {
            EnsureContent();
            ProcessFrontMatter();   //Make sure the FM is processed
            RemoveFrontMatter();    //This is a separate step because FM can be cached and it's only dependent of the current file
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

            //If cache is not enabled or the FM is not currently cached, read it from the file contents
            //Default value (empty FM, but no empty string), prevents the Content property from processing Front-Matter twice if it's not read yet
            _FrontMatter = new SimpleYAMLParser(strFM);

            //Extract and remove YAML Front Matter
            EnsureContent();
            Match fm = FRONT_MATTER_RE.Match(this._content);
            if (fm.Length > 0) //If there's front matter available
            {
                strFM = fm.Groups[0].Value;
                //Save front matter text
                _FrontMatter = new SimpleYAMLParser(strFM);
            }

            //Cache the final FM contents (if caching is enabled)
            AddToCache(this.CachingIDFrontMatter, strFM);
        }

        //Removes the front matter, if any, from the actual contents of the file
        private void RemoveFrontMatter()
        {
            _content = FRONT_MATTER_RE.Replace(_content, "");
        }
        #endregion
    }
}
