using System;
using System.Web;
using System.Web.Configuration;
using System.Web.Caching;
using System.IO;
using System.Text.RegularExpressions;
using Markdig;


namespace IISMarkdownHandler
{

    /// <summary>
    /// Loads and processes a markdown file
    /// </summary>
    public class MarkdownFile
    {

        #region Constructor
        //Reads and process the file. 
        //IMPORTANT: Expects the PHYSICAL path to the file.
        //Possibly generates errors that must be handled in the call-stack
        public MarkdownFile(string mdFilePath)
        {
            //Read the file contents from disk or cache depending on parameter
            if (WebConfigurationManager.AppSettings["UseMDCaching"] == "1")
            {
                Content = Helper.readTextFromFileWithCaching(mdFilePath);   //Use cache
                HTML = HttpRuntime.Cache[mdFilePath + "_HTML"] as string;   //Try to read HTML result from cache
                if (string.IsNullOrEmpty(HTML)) //If it's not in the cache, transform it
                {
                    HTML = MarkdownToHtml(Content);
                    HttpRuntime.Cache.Insert(mdFilePath + "_HTML", HTML, new CacheDependency(mdFilePath)); //Add result to cache with dependency on the file
                }
            }
            else
            {
                Content = Helper.readTextFromFile(mdFilePath);  //Always read from disk
                //Convert to HTML
                HTML = MarkdownToHtml(Content);
            }
            //Get markdown file information
            FileInfo fi = new FileInfo(mdFilePath);
            FileName = fi.Name;
            DateCreated = fi.CreationTime;
            DateLastModified = fi.LastWriteTime;
            //Try to get the title of the file from the contents (find the first H1 if there's any)
            //TODO: Quick and dirty with RegExp and only with "#". Improve using the Markdown parser
            Regex re = new Regex(@"^\s*?#\s(.*)$", RegexOptions.Multiline);
            if (re.IsMatch(Content))
                Title = re.Matches(Content)[0].Groups[1].Captures[0].Value;
            else
                Title = new FileInfo(mdFilePath).Name;
        }
        #endregion

        #region Properties
        public string FileName { get; private set; } //The file name
        public string Content { get; private set; } //The file contents
        public string HTML { get; private set; }    //The HTML generated from the markdown contents
        public string Title { get; private set; }   //The title of the file (first available H1 header or the file name)
        public DateTime DateCreated { get; private set; }   //Date when the file was created
        public DateTime DateLastModified { get; private set; }  //Date when the file was last modified
        #endregion

        #region Helper methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="markdown"></param>
        /// <returns></returns>
        private string MarkdownToHtml(string markdown)
        {
            //Configure markdown conversion
            MarkdownPipelineBuilder mdPipe = new MarkdownPipelineBuilder().UseAdvancedExtensions();
            //Check if we must generate emojis
            if (WebConfigurationManager.AppSettings["UseEmoji"] != "0")
            {
                mdPipe = mdPipe.UseEmojiAndSmiley();
            }
            var pipeline = mdPipe.Build();
            //Convert markdown to HTML
            return Markdig.Markdown.ToHtml(markdown, pipeline); //Converto to HTML
        }
        #endregion
    }
}