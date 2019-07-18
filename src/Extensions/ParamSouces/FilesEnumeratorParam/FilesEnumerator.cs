using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Linq;
using MIISHandler;

namespace FilesEnumeratorParam
{
    /// <summary>
    /// A helper enum to specify the sort order of the resulting files
    /// </summary>
    public enum SortDirection
    {
        desc = 0,
        asc = 1
    }

    /// <summary>
    /// Helper class to retrieve files from folder in different ways
    /// </summary>
    public static class FilesEnumerator
    {
        //Valid file extensions - Only MIIS files (.md or .mdh)
        private static readonly string[] VALID_EXTS = new string[] { MarkdownFile.MARKDOWN_DEF_EXT, MarkdownFile.HTML_EXT };
        private static readonly string[] EXCLUDED_FILE_NAMES = new string[] { "index", "default" };

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
        /// Returns all the files in an specific folder, excluding files begining with "_" and including only .md or .mdh files.
        /// </summary>
        /// <param name="folderPath">The full path to the folder that contains the files</param>
        /// <param name="topFolderOnly">If true the results will include files into subfolders too</param>
        /// <param name="sortdirection">The sort direction ordering the files by date.</param>
        /// <returns></returns>
        public static IEnumerable<MarkdownFile> GetAllFilesFromFolder(string folderPath, bool topFolderOnly, SortDirection sortdirection)
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
                            where !file.Name.StartsWith("_") && VALID_EXTS.Contains(file.Extension.ToLower()) && !EXCLUDED_FILE_NAMES.Contains(Path.GetFileNameWithoutExtension(file.Name).ToLower())
                            select new MarkdownFile(file.FullName)
                           );

            var allFilesSorted = (sortdirection == SortDirection.desc) ? 
                                 allFiles.OrderByDescending<MarkdownFile, DateTime>(f => f.Date) : //First the newest
                                 allFiles.OrderBy<MarkdownFile, DateTime>(f => f.Date); //First the oldest

            return allFilesSorted;
        }
    }
}
