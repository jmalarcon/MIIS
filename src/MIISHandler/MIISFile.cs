using System;
using System.IO;
using System.Linq;

namespace MIISHandler
{
    /// <summary>
    /// Proxy class to an underlying processed file to expose oly a few features through a DotLiquid context or to extensions
    /// </summary>
    public class MIISFile : DotLiquid.Drop  //Implements Drop to be able to be used in templates (by custom tags)
    {
        //Reference to the internal MD or MDF file
        private MarkdownFile md;
        public  MIISFile(MarkdownFile mdFile)
        {
            md = mdFile;
        }

        /// <summary>
        /// Current file's name
        /// </summary>
        public string FileName
        {
            get
            {
                return md.FileName;
            }
        }

        /// <summary>
        /// Current file's absolute path in disk
        /// </summary>
        public string FilePath
        {
            get
            {
                return md.FilePath;
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
                return IISHelpers.WebHelper.GetAbsolutePath(this.FilePath);
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
                return md.IsPublished;
            }
        }

        /// <summary>
        /// Title of the page
        /// </summary>
        public string Title
        {
            get
            {
                return md.Title;
            }
        }

        /// <summary>
        /// Excerpt for the page if present in the Front-Matter. It looks for the fields: excerpt, description & summary, in that order of precedence
        /// </summary>
        public string Excerpt
        {
            get
            {
                return md.Excerpt;
            }
        }

        /// <summary>
        /// The rendered HTML content of the page
        /// </summary>
        public string HTML
        {
            get
            {
                return md.HTML;
            }
        }

        /// <summary>
        /// Returns the value of the Date field if present or the creation date if not
        /// </summary>
        public DateTime Date
        {
            get
            {
                return md.Date;
            }
        }

        /// <summary>
        /// Returns Categories for this file if the field is present. Otherwise, returns an empty string array
        /// </summary>
        public string[] Categories
        {
            get
            {
                string sCategs = FieldValuesHelper.GetFieldValue("categories", md);
                return sCategs.Split(',').Select(c => c.Trim().ToLowerInvariant()).Where(c => !string.IsNullOrEmpty(c)).ToArray<string>();
            }
        }

        /// <summary>
        /// Returns Categories for this file if the field is present. Otherwise, returns an empty string array
        /// </summary>
        public string[] Tags
        {
            get
            {
                string sTags = FieldValuesHelper.GetFieldValue("tags", md);
                return sTags.Split(',').Select(c => c.Trim().ToLowerInvariant()).Where(c => !string.IsNullOrEmpty(c)).ToArray<string>();
            }
        }

        /// <summary>
        /// Returns the value of the indicated parameter or the default value
        /// </summary>
        /// <param name="name">The name of the parameter that we want to get. Checks</param>
        /// <param name="defvalue">The default value for the parameter if it's not present</param>
        /// <returns>A string with the value for the parameter. Get's it first from the Front Matter and if it's not present, 
        /// from the global values in web.config</returns>
        public string GetFMValue(string name, string defvalue)
        {
            return FieldValuesHelper.GetFieldValue(name, this.md, defvalue);
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
            md.CachingEnabled = false;
        }

        /// <summary>
        /// Allows a custom tag or FM source to add a new file or folder dependency to the current MD or MDH file caching
        /// </summary>
        /// <param name="filePath">The full path to the file or folder that the current file depends on</param>
        public void AddFileDependency(string filePath)
        {
            //If is a valid file or folder and it's not already added to the dependencies, add it
            if ( 
                (File.Exists(filePath) || Directory.Exists(filePath)) 
                 && !md.Dependencies.Contains(filePath) 
               )
                md.Dependencies.Add(filePath);
        }

        /// <summary>
        /// Allows the custom tag or param to add a number of seconds of validity to the file cache 
        /// so that it can be fresh in reasonable spans of time
        /// </summary>
        /// <param name="seconds">The value must be between 1 second a 24 hours (86400 seconds). Will force this range.</param>
        public void SetMaxCacheValidity(double seconds)
        {
            md.NumSecondsCacheIsValid = seconds;
        }

        #endregion
    }
}