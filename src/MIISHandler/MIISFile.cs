using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Caching;

namespace MIISHandler
{
    /// <summary>
    /// Proxy class to an underlying processed file to expose oly a few features through a DotLiquid context or to extensions
    /// </summary>
    public class MIISFile : DotLiquid.Drop  //Implements Drop to be able to be used in templates (by custom tags)
    {
        //Reference to the internal MD or MDF file
        private readonly MarkdownFile _md;
        //URL
        private string _url = "";
        //Tags and categories
        private string[] _tags = null;
        private string[] _categories = null;

        public  MIISFile(MarkdownFile mdFile)
        {
            _md = mdFile;
        }

        /// <summary>
        /// Gets any property for the file, specificaly defined (such as Title, Tags Categories...) or from the Front-Matter
        /// It won't process any special FM value such as File Processing Fields or Custom Front Matter Values
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public override object this[object fieldName]
        {
            get
            {
                object res = base[fieldName];
                if (res == null)
                    res = FieldValuesHelper.GetFieldObject(fieldName.ToString(), _md, null);

                return res;
            }
        }

        /// <summary>
        /// Gets if a specified property for the file exists or not, specificaly defined (such as Title, Tags Categories...) or from the Front-Matter
        /// It won't process any special FM value such as File Processing Fields or Custom Front Matter Values
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public override bool ContainsKey(object fieldName)
        {
            bool hasProp = base.ContainsKey(fieldName);
            if (!hasProp)
            {
                object propVal = FieldValuesHelper.GetFieldObject(fieldName.ToString(), _md, null);
                if (propVal != null)
                    hasProp = true;
            }
            return hasProp;
        }

        /// <summary>
        /// Current file's name
        /// </summary>
        public string FileName
        {
            get
            {
                return _md.FileName;
            }
        }

        /// <summary>
        /// Current file's name without extension
        /// </summary>
        public string FileNameNoExt
        {
            get
            {
                return _md.FileNameNoExt;
            }
        }

        /// <summary>
        /// Current file's absolute path in disk
        /// </summary>
        public string FilePath
        {
            get
            {
                return _md.FilePath;
            }
        }

        /// <summary>
        /// Current file's containing folder path from the root of the site
        /// </summary>
        public string Dir
        {
            get
            {
                return IISHelpers.WebHelper.GetContainingDir(this.URL);
            }
        }

        /// <summary>
        /// The current file URL from the root of the website
        /// It DOESN'T WORK with files inside virtual folders. Must be physical folders.
        /// i.e: /posts/post-file.name.md
        /// </summary>
        public string URL
        {
            get
            {
                if (_url == "")
                {
                    string absPath = IISHelpers.WebHelper.GetAbsolutePath(this.FilePath);
                    //Remove the file name if it's a default one (index.md(h) or default.md(h)
                    _url = Regex.Replace(absPath, @"^(.*\/)(index|default)(\.[^\/]+)$", "$1", RegexOptions.IgnoreCase);
                }
                return _url;
            }
        }

        /// <summary>
        /// The current file URL from the root of the website, without the file extension
        /// </summary>
        public string URLNoExt
        {
            get
            {
                return IISHelpers.IOHelper.RemoveFileExtension(this.URL);
            }
        }

        /// <summary>
        /// The file's unique ID. It's equivalent to the URL property
        /// </summary>
        public string id
        {
            get
            {
                return this.URL;
            }
        }


        /// <summary>
        /// Indicates if the current post is published or not depending on the Published field and the date
        /// </summary>
        public bool Published
        {
            get
            {
                return _md.IsPublished;
            }
        }

        /// <summary>
        /// Title of the page
        /// </summary>
        public string Title
        {
            get
            {
                return _md.Title;
            }
        }

        /// <summary>
        /// Excerpt for the page if present in the Front-Matter. It looks for the fields: excerpt, description & summary, in that order of precedence
        /// If none is found, then gets the first paragraph in the contents (Markdown or HTML)
        /// IMPORTANT: since this last option will trigger reading the file contents from disk and process all tags is highly recommended to add one
        /// of the valid fields to files whose excerpt porperty we plan to use in other files, i.e, posts from a blog or other similar files.
        /// </summary>
        public string Excerpt
        {
            get
            {
                return _md.Excerpt;
            }
        }

        /// <summary>
        /// Alias for Excerpt
        /// </summary>
        public string Description
        {
            get
            {
                return this.Excerpt;
            }
        }

        /// <summary>
        /// Alias for Excerpt
        /// </summary>
        public string Summary
        {
            get
            {
                return this.Excerpt;
            }
        }

        /// <summary>
        /// The rendered HTML content of the page, without the template (just raw html, processed)
        /// </summary>
        public string Html
        {
            get
            {
                return _md.RawFinalHtml;
            }
        }

        /// <summary>
        /// Returns the value of the Date field if present or the creation date if not
        /// </summary>
        public DateTime Date
        {
            get
            {
                return _md.Date;
            }
        }

        /// <summary>
        /// Returns Categories for this file if the field is present. Otherwise, returns an empty string array
        /// </summary>
        public string[] Categories
        {
            get
            {
                if (_categories == null)
                {
                    var guessedValue = FieldValuesHelper.GetFieldObject("categories", _md);
                    if (!(guessedValue is Array))
                        _categories = new string[0];
                    else
                        _categories = ((string[]) guessedValue).Select(c => c.Trim().ToLowerInvariant()).Where(c => !string.IsNullOrEmpty(c)).ToArray<string>();    //To Lowercase
                }
                return _categories;
            }
        }

        /// <summary>
        /// Alias for the Categories property for Jekyll templates compatibility
        /// </summary>
        public string[] Category
        {
            get
            {
                return this.Categories;
            }
        }

        /// <summary>
        /// Returns Tags for this file if the field is present. Otherwise, returns an empty string array
        /// </summary>
        public string[] Tags
        {
            get
            {
                if (_tags == null)
                {
                    var guessedValue = FieldValuesHelper.GetFieldObject("tags", _md);
                    if (!(guessedValue is Array))
                        _tags = new string[0];
                    else
                        _tags = ((string[]) guessedValue).Select(c => c.Trim().ToLowerInvariant()).Where(c => !string.IsNullOrEmpty(c)).ToArray<string>();  //To lowercase
                }
                return _tags;
            }
        }

        //Current Template name
        public string TemplateName
        {
            get
            {
                return _md.TemplateName;
            }
        }

        //Current layout file name
        public string Layout
        {
            get
            {
                return _md.Layout;
            }
        }

        #region Caching proxies

        /// <summary>
        /// Disables caching for the current file. 
        /// This should be only used in custom tags or parameters that depend on fresh real-time values. 
        /// A time based caching is normally preferred if we need fresh values but they are not so critical that 
        /// every single request should have them retrieved
        /// </summary>
        public void DisableCache()
        {
            _md.CachingEnabled = false;
        }

        /// <summary>
        /// Allows a custom tag or FM source to add a new file or folder dependency to the current MD or MDH file caching
        /// </summary>
        /// <param name="filePath">The full path to the file or folder that the current file depends on</param>
        public void AddFileDependency(string filePath)
        {
            _md.AddFileDependency(filePath);
        }

        /// <summary>
        /// Allows a custom tag or FM source to add a new CacheDependency class to the current MD or MDH file caching
        /// It is combined with other dependencies that may exist
        /// It's for specialized caching strategies only
        /// </summary>
        /// <param name="cd">CacheDependency object (or an inherited class)</param>
        public void AddFileDependency(CacheDependency cd)
        {
            //Just delegates this to the underlying file
            _md.AddCacheDependency(cd);
        }

        /// <summary>
        /// Allows the custom tag or param to add a number of seconds of validity to the file cache 
        /// so that it can be fresh in reasonable spans of time
        /// </summary>
        /// <param name="seconds">The value must be between 1 second a 24 hours (86400 seconds). Will force this range.</param>
        public void SetCachingTimeOut(double seconds)
        {
            _md.CachingTimeOut = seconds;
        }

        #endregion
    }
}