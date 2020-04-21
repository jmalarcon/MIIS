using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Caching;
using System.Linq;
using MIISHandler;

namespace MIISFilesEnumeratorFMS
{
    /// <summary>
    /// Helper class to retrieve files from folder in different ways
    /// </summary>
    internal static class FilesEnumeratorHelper
    {
        #region constants and private members
        //MIIS file extensions - Only MIIS files (.md or .mdh)
        internal static readonly string[] MIIS_EXTS = new string[] { MarkdownFile.MARKDOWN_DEF_EXT, MarkdownFile.HTML_EXT };
        private static readonly string[] EXCLUDED_FILE_NAMES = new string[] { "index", "default" };
        #endregion

        #region File management methods
        /// <summary>
        /// Returns all the files in an specific folder, excluding files begining with "_" and including only .md or .mdh files.
        /// </summary>
        /// <param name="folderPath">The full path to the folder that contains the files</param>
        /// <param name="topFolderOnly">If true the results will include files into subfolders too</param>
        /// <returns>Returns files in the default order they have in disk. NO further ordering is done</returns>
        private static IEnumerable<MarkdownFile> GetAllFilesFromFolderInternal(string folderPath, bool topFolderOnly, 
            string[] validExtensions = null, string[] excludedFileNames = null)
        {
            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException("Folder does not exist!!");
                //return new List<MarkdownFile>();
            }

            //Search top directory only or all subdirectories too
            SearchOption sfo = topFolderOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;

            DirectoryInfo di = new DirectoryInfo(folderPath);

            //By default use only valid MIIS extensions
            if (validExtensions == null)
                validExtensions = MIIS_EXTS;

            //By default exclude default names such as "index" or "default"
            if (excludedFileNames == null)
                excludedFileNames = EXCLUDED_FILE_NAMES;

            var allFiles = (from file in di.EnumerateFiles("*.*", sfo)
                                //Include only MIIS files and exclude files that start with "_" or with a default name (index or default)
                            where !file.Name.StartsWith("_") && validExtensions.Contains(file.Extension.ToLowerInvariant()) && 
                                  !excludedFileNames.Contains(Path.GetFileNameWithoutExtension(file.Name).ToLowerInvariant())
                            select new MarkdownFile(file.FullName)
                           );

            return allFiles;
        }

        /// <summary>
        /// Returns all the files in an specific folder, excluding files begining with "_" and including only .md or .mdh files.
        /// </summary>
        /// <param name="folderPath">The full path to the folder that contains the files</param>
        /// <param name="topFolderOnly">If true the results will include files into subfolders too</param>
        /// <param name="sortdirection">The sort direction ordering the files by date.</param>
        /// <returns></returns>
        public static IEnumerable<MarkdownFile> GetAllFilesFromFolder(string folderPath, bool topFolderOnly)
        {
            //Try to read from the results cache
            string cacheKey = folderPath + "_files" + "_" + topFolderOnly;
            IEnumerable<MarkdownFile> allFiles = HttpRuntime.Cache[cacheKey] as IEnumerable<MarkdownFile>;

            if (allFiles == null)   //Read files from disk if not in the cache
            {
                allFiles = GetAllFilesFromFolderInternal(folderPath, topFolderOnly).OrderByDescending<MarkdownFile, DateTime>(f => f.Date);   //Oldest file first (this is the most common way to use them)
                //Add sorted files to cache depending on the folder and the time until the next published file
                CacheResults(folderPath, cacheKey, NumSecondsToNextFilePubDate(allFiles), allFiles);
            }

            //Return all the files sorted by date desc (is the default sort order)
            return allFiles;
        }

        /// <summary>
        /// Returns an IEnumerable list of MIISFiles which only includes published files
        /// </summary>
        /// <param name="allFiles">A full lst of files</param>
        /// <returns></returns>
        public static IEnumerable<MIISFile> OnlyPublished(IEnumerable<MarkdownFile> allFiles)
        {
            return from file in allFiles
                   where file.IsPublished
                   select new MIISFile(file);
        }

        #endregion

        #region Caching
        /// <summary>
        /// Adds cache dependencies for the source file depending on the current files being processed
        /// </summary>
        /// <param name="currentFile"></param>
        /// <param name="folderPath"></param>
        /// <param name="allFiles"></param>
        public static void AddCacheDependencies(MIISFile currentFile, string folderPath, IEnumerable<MarkdownFile> allFiles)
        {
            //Establish the processed folder as a caching dependency for the current file this FM souce is working on
            currentFile.AddFileDependency(folderPath);
            //If any of the files has a (publishing) date later than now, then add the first one as a cache dependency to refresh the listings at that point
            double maxDateSecs = NumSecondsToNextFilePubDate(allFiles);
            if (maxDateSecs > 0)
                currentFile.SetMaxCacheValidity(maxDateSecs);
        }

        /// <summary>
        /// Returns the number of seconds from now to the publish date for the next file not published yet
        /// checking all files publishing dates (if any).
        /// </summary>
        /// <param name="allFiles">An IEnumerable list of files</param>
        /// <returns>Returns 0 if there is no file scheduled to be published in the future</returns>
        public static double NumSecondsToNextFilePubDate(IEnumerable<MarkdownFile> allFiles)
        {
            double maxDateSecs = 0;
            var futureFilesDates = from f in allFiles
                                   where f.Date > DateTime.Now
                                   select f.Date;
            if (futureFilesDates.Count() > 0)
            {
                maxDateSecs = ((futureFilesDates).Min<DateTime>() - DateTime.Now).TotalSeconds;
            }

            return maxDateSecs;
        }

        /// <summary>
        /// Adds the object to the cache
        /// </summary>
        /// <param name="folderPath">The folder to monitor for changes to invalidate the cache</param>
        /// <param name="cacheKey">The cache identifer</param>
        /// <param name="maxDateSecs">Maximum number of seconds to keep the cache alive</param>
        /// <param name="result">The result to cache</param>
        public static void CacheResults(string folderPath, string cacheKey, double maxDateSecs, object result)
        {
            //If any of the files has a (publishing) date later than now, then add the first one as a cache dependency to refresh the cache at that point
            if (maxDateSecs > 0)    //Add dependency with a maximum period of validity
                HttpRuntime.Cache.Insert(cacheKey, result, new CacheDependency(folderPath),
                    DateTime.UtcNow.AddSeconds(maxDateSecs), Cache.NoSlidingExpiration);
            else  //Just add the folder as a dependency
                HttpRuntime.Cache.Insert(cacheKey, result, new CacheDependency(folderPath));
        }
        #endregion

        #region Other generic aux methods
        /// <summary>
        /// Returns the path in the file system from the relative path of a url
        /// </summary>
        /// <param name="folderRelPath">Relative url path, such as "./", "/folder/" or "../posts/"</param>
        /// <returns></returns>
        public static string GetFolderAbsPathFromName(string folderRelPath)
        {
            //Check if there's a folder specified
            string folder = folderRelPath.Trim();
            HttpContext ctx = HttpContext.Current;
            //Return absolute path for folder
            string folderAbsPath = ctx.Server.MapPath(folder);
            return folderAbsPath;
        }

        /// <summary>
        /// Determines if a string value (normally got from params or Front-Matter)
        /// representes a "true" value or not.
        /// Valid "true" values are: "1", "true" and "yes"
        /// </summary>
        /// <param name="val">The value to be checked</param>
        /// <returns>true if is a truthy value according to the criteria</returns>
        internal static bool IsTruthy(string val)
        {
            val = val.ToLowerInvariant();
            return (val == "1" || val == "true" || val == "yes");
        }

        /// <summary>
        /// Determines if a string value (normally got from params or Front-Matter)
        /// representes a "false" value or not.
        /// Valid "false" values are: "0", "false" and "no"
        /// </summary>
        /// <param name="val">The value to be checked</param>
        /// <returns>true if is a truthy value according to the criteria</returns>
        internal static bool IsFalsy(string val)
        {
            return (val == "0" || val == "false" || val == "no");
        }

        #endregion
    }
}
