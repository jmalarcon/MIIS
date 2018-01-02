using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Caching;
using System.IO;
using System.Text.RegularExpressions;
using Markdig;


namespace MIISHandler
{

    /// <summary>
    /// Loads and processes a markdown file
    /// </summary>
    public class MarkdownFile
    {

        public const string HTML_EXT = ".mdh";

        #region private fields
        string _content;
        string _rawHtml;
        string _html;
        string _title;
        string _filename;
        DateTime _dateCreated;
        DateTime _dateLastModified;
        #endregion

        #region Constructor
        //Reads and process the file. 
        //IMPORTANT: Expects the PHYSICAL path to the file.
        //Possibly generates errors that must be handled in the call-stack
        public MarkdownFile(string mdFilePath)
        {
            this.FilePath = mdFilePath;
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
                if (string.IsNullOrEmpty(_content))
                    _content = Helper.readTextFromFile(this.FilePath);

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
                    else  //Is markdown
                    {
                        //Configure markdown conversion
                        MarkdownPipelineBuilder mdPipe = new MarkdownPipelineBuilder().UseAdvancedExtensions();
                        //Check if we must generate emojis
                        if (Helper.GetParamValue("UseEmoji", "1") != "0")
                        {
                            mdPipe = mdPipe.UseEmojiAndSmiley();
                        }
                        var pipeline = mdPipe.Build();
                        //Convert markdown to HTML
                        _rawHtml = Markdig.Markdown.ToHtml(this.Content, pipeline); //Converto to HTML
                    }

                    //Transform virtual paths before returning
                    _rawHtml = Helper.TransformVirtualPaths(_rawHtml);
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
                    if (Helper.GetParamValue("UseMDCaching", "1") == "1")
                    {
                        //The common case: cache enabled. 
                        //Try to read from cache
                        _html = HttpRuntime.Cache[this.FilePath + "_HTML"] as string;
                        if (string.IsNullOrEmpty(_html)) //If it's not in the cache, transform it
                        {
                            //Initialize the file dependencies
                            this.Dependencies = new List<string>();
                            this.Dependencies.Add(this.FilePath);   //Add current file as cache dependency (the render process will add the fragments if needed)
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

                if (this.FileExt == HTML_EXT)  //If it's just HTML
                {
                    //Use the file name, with no extension, as the title
                    _title = Path.GetFileNameWithoutExtension(this.FileName);
                }
                else
                {
                    //Try to get the title of the file from the contents (find the first H1 if there's any)
                    //TODO: Quick and dirty with RegExp and only with "#".
                    Regex re = new Regex(@"^\s*?#\s(.*)$", RegexOptions.Multiline);
                    if (re.IsMatch(this.Content))
                        _title = re.Matches(this.Content)[0].Groups[1].Captures[0].Value;
                    else
                        _title = Path.GetFileNameWithoutExtension(this.FileName);
                }

                return _title;
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
        internal List<string> Dependencies { get; set; }
        #endregion
    }
}