using System;
using System.IO;

namespace MIISHandler
{
    /// <summary>
    /// Proxy class to an underlying processed file to expose oly a few features through a DotLiquid context
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
        /// Allows a custom tag to add a new file dependency to the current MD or MDH file caching
        /// </summary>
        /// <param name="filePath">The full path to the file that the current file depends on</param>
        public void AddFileDependency(string filePath)
        {
            //If is a valid file and it's not already added to the dependencies, add it
            if (File.Exists(filePath) && !md.Dependencies.Contains(filePath))
                md.Dependencies.Add(filePath);
        }

        /// <summary>
        /// Allows the custom tag or param to add a number of seconds of validity to the file cache 
        /// so that it can be fresh in reasonable spans of time
        /// </summary>
        /// <param name="seconds">The value must be between 1 second a 24 hours (86400 seconds). Will force this range.</param>
        public void SetMaxCacheValidity(int seconds)
        {
            md.NumSecondsCacheIsValid = seconds;
        }
    }
}