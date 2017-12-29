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

        public const string HTML_EXT = ".mdh";

        #region Constructor
        //Reads and process the file. 
        //IMPORTANT: Expects the PHYSICAL path to the file.
        //Possibly generates errors that must be handled in the call-stack
        public MarkdownFile(string mdFilePath)
        {
            //Get markdown file information
            FileInfo fi = new FileInfo(mdFilePath);
            this.FileName = fi.Name;
            this.FileExt = fi.Extension.ToLower();
            this.DateCreated = fi.CreationTime;
            this.DateLastModified = fi.LastWriteTime;

            //Read the file contents from disk or cache depending on parameter
            if (Helper.GetParamValue("UseMDCaching", "1") == "1")
            {
                this.Content = Helper.readTextFromFileWithCaching(mdFilePath);   //Use cache
                this.HTML = HttpRuntime.Cache[mdFilePath + "_HTML"] as string;   //Try to read HTML result from cache
                if (string.IsNullOrEmpty(this.HTML)) //If it's not in the cache, transform it
                {
                    this.HTML = MarkdownToHtml();
                    HttpRuntime.Cache.Insert(mdFilePath + "_HTML", this.HTML, new CacheDependency(mdFilePath)); //Add result to cache with dependency on the file
                }
            }
            else
            {
                this.Content = Helper.readTextFromFile(mdFilePath);  //Always read from disk
                //Convert to HTML
                this.HTML = MarkdownToHtml();
            }

            if (this.FileExt == HTML_EXT)  //If it's just HTML
            {
                //Use the file name, with no extension, as the title
                this.Title = Path.GetFileNameWithoutExtension(this.FileName);
            }
            else
            { 
                //Try to get the title of the file from the contents (find the first H1 if there's any)
                //TODO: Quick and dirty with RegExp and only with "#".
                Regex re = new Regex(@"^\s*?#\s(.*)$", RegexOptions.Multiline);
                if (re.IsMatch(this.Content))
                    this.Title = re.Matches(this.Content)[0].Groups[1].Captures[0].Value;
                else
                    this.Title = Path.GetFileNameWithoutExtension(this.FileName);
            }
        }
        #endregion

        #region Properties
        public string FileName { get; private set; } //The file name
        public string FileExt { get; set; } //The file extrension (with dot)
        public string Content { get; private set; } //The file contents
        public string HTML { get; private set; }    //The HTML generated from the markdown contents
        public string Title { get; private set; }   //The title of the file (first available H1 header or the file name)
        public DateTime DateCreated { get; private set; }   //Date when the file was created
        public DateTime DateLastModified { get; private set; }  //Date when the file was last modified
        #endregion

        #region Helper methods

        /// <summary>
        /// Takes current Markdown file content and transforms it to HTML using Markdig
        /// </summary>
        /// <returns></returns>
        private string MarkdownToHtml()
        {
            string html;
            //Check if its a pure HTML file (.mdh extension)
            if (this.FileExt == HTML_EXT)  //It's HTML
            {
                //No transformation required --> It's an HTML file processed by the handler to mix with the current template
                html = this.Content;
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
                html = Markdig.Markdown.ToHtml(this.Content, pipeline); //Converto to HTML
            }

            //Transform virtual paths before returning
            return Helper.TransformVirtualPaths(html);
        }
        #endregion
    }
}