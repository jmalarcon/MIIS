using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Web;
using System.Linq;
using MIISHandler;

namespace FilesEnumeratorParam
{
    /// <summary>
    /// A helper enum to specify the sort order of the resulting files
    /// </summary>
    internal enum SortDirection
    {
        desc = 0,
        asc = 1
    }

    /// <summary>
    /// Helper class to retrieve files from folder in different ways
    /// </summary>
    internal static class FilesEnumeratorHelper
    {
        #region constants and private members
        //Valid file extensions - Only MIIS files (.md or .mdh)
        private static readonly string[] VALID_EXTS = new string[] { MarkdownFile.MARKDOWN_DEF_EXT, MarkdownFile.HTML_EXT };
        private static readonly string[] EXCLUDED_FILE_NAMES = new string[] { "index", "default" };
        #endregion

        #region File management methods
        /// <summary>
        /// Returns all the files in an specific folder, excluding files begining with "_" and including only .md or .mdh files.
        /// </summary>
        /// <param name="folderPath">The full path to the folder that contains the files</param>
        /// <param name="topFolderOnly">If true the results will include files into subfolders too</param>
        /// <returns>Returns files in the dafult order they have in disk. NO further ordering is done</returns>
        public static IEnumerable<MarkdownFile> GetAllFilesFromFolder(string folderPath, bool topFolderOnly)
        {
            if (!Directory.Exists(folderPath))
            {
                return null;
            }

            //Search top directory only or all subdirectories too
            SearchOption sfo = topFolderOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;

            DirectoryInfo di = new DirectoryInfo(folderPath);

            var allFiles = (from file in di.EnumerateFiles("*.*", sfo)
                                //Include only MIIS files and exclude files that start with "_" or with a default name (index or default)
                            where !file.Name.StartsWith("_") && VALID_EXTS.Contains(file.Extension.ToLowerInvariant()) && !EXCLUDED_FILE_NAMES.Contains(Path.GetFileNameWithoutExtension(file.Name).ToLowerInvariant())
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
        public static IEnumerable<MarkdownFile> GetAllFilesFromFolder(string folderPath, bool topFolderOnly, SortDirection sortdirection)
        {
            var allFiles = GetAllFilesFromFolder(folderPath, topFolderOnly);

            var allFilesSorted = (sortdirection == SortDirection.desc) ? 
                                 allFiles.OrderByDescending<MarkdownFile, DateTime>(f => f.Date) : //First the newest
                                 allFiles.OrderBy<MarkdownFile, DateTime>(f => f.Date); //First the oldest

            return allFilesSorted;
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
            //If any of the files has a (publishing) date later than now, then add that as a cache dependency to refresh the listings at that point
            double maxDateSecs = ((from f in allFiles select f.Date).Max<DateTime>() - DateTime.Now).TotalSeconds;
            if (maxDateSecs > 0)
                currentFile.SetMaxCacheValidity(maxDateSecs);
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
