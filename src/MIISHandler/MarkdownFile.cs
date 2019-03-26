﻿using System;
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

        public const string HTML_EXT = ".mdh";  //File extension for HTML contents
        private readonly Regex FRONT_MATTER_RE = new Regex(@"^-{3,}(.*?)-{3,}", RegexOptions.Singleline);  //It allows more than 3 dashed to be used to delimit the Front-Matter (the YAML spec requires exactly 3 dashes, but I like to allow more freedom on this, so 3 or more in a line is allowed)

        #region private fields
        private string _content = "";
        private string _rawHtml;
        private string _html;
        private string _title;
        private string _filename;
        private DateTime _dateCreated;
        private DateTime _dateLastModified;
        private SimpleYAMLParser _FrontMatter;
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
                        if (Common.GetFieldValue("UseEmoji", this, "1") != "0")
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
                if (string.IsNullOrEmpty(_html))
                {
                    //Read the file contents from disk or cache depending on parameter
                    if (Common.GetFieldValue("UseMDCaching", null, "1") == "1")
                    {
                        //The common case: cache enabled. 
                        //Try to read from cache
                        _html = HttpRuntime.Cache[this.FilePath + "_HTML"] as string;
                        if (string.IsNullOrEmpty(_html)) //If it's not in the cache, transform it
                        {
                            _html = HTMLRenderer.RenderMarkdown(this);
                            HttpRuntime.Cache.Insert(this.FilePath + "_HTML", _html, new CacheDependency(this.Dependencies.ToArray())); //Add result to cache with dependency on the file
                        }
                    }
                    else
                    {
                        //If the cache is disabled always re-process the file
                        _html = HTMLRenderer.RenderMarkdown(this);
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
                if (_dateCreated != default(DateTime))
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
                if (_dateLastModified != default(DateTime))
                    return _dateLastModified;

                FileInfo fi = new FileInfo(this.FilePath);
                _dateLastModified = fi.LastWriteTime;
                return _dateLastModified;
            }
        }

        //The file paths of files the current file depends on, including itself (current file + fragments)
        internal List<string> Dependencies { get; private set; }

        //If the file is published or is explicitly forbidden by the author using the "Published" field
        internal bool IsPublished
        {
            get
            {
                //Check if the File is published with the "Published" field
                string isPublished = Common.GetFieldValue("Published", this, "1").ToLower();
                //For the sake of security, if it's not explicitly a "falsy" value, then is assumed true
                return !TypesHelper.IsFalsy(isPublished);
            }
        }

        //Determines a special status code to return for the file (default is 200, OK)
        //Valid status codes: https://docs.microsoft.com/en-us/windows/desktop/WinHttp/http-status-codes
        internal int HttpStatusCode
        {
            get
            {
                //Check if the File has a status code that is not a 200 (default OK status code)
                string statusCode = Common.GetFieldValueFromFM("HttpStatusCode", this, "200").ToLower();
                bool isInt = int.TryParse(statusCode, out int nStatus);
                if (!isInt) nStatus = 200;
                return Math.Abs(nStatus);
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
            bool cacheEnabled = Common.GetFieldValue("UseMDCaching", null, "1") == "1";

            if (cacheEnabled)   //If the file cache is enabled
            {
                strFM = HttpRuntime.Cache[this.FilePath + "_FM"] as string;  //Try to read Front-Matter from cache
                if (!string.IsNullOrEmpty(strFM)) //If it in the cache, use it
                {
                    _FrontMatter = new SimpleYAMLParser(strFM);
                    return;
                }
                else
                {
                    strFM = "---\r\n---";   //Re-set to an empty FrontMatter (if I use an empty string it would be reading contents from this for all the files without Front Matter
                }
            }

            //If cache is not enabled or the FM is not currently cached, read from contents
            //Default value (empty), prevents Content property from processing Front-Matter twice if it's still not read
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

            //Cache FM contents if caching is enabled
            if (cacheEnabled)
            {
                HttpRuntime.Cache.Insert(this.FilePath + "_FM", strFM, new CacheDependency(this.FilePath)); //Add FM to cache with dependency on the current MD/MDH file
            }
        }

        //Removes the front matter, if any, from the current contents
        private void RemoveFrontMatter()
        {
            _content = FRONT_MATTER_RE.Replace(_content, "");
        }
        #endregion
    }
}
