using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;
using MIISFilesEnumeratorFMS;

namespace MIISHandler.FMSources
{
    public class CategsFolderParamSrc : IFMSource
    {
        string IFMSource.SourceName => "CategsFromFolder";

        /// <summary>
        /// Returns an array with all the distinct categories available in the files of the specified folder ordered by category name (ascending).
        /// Includes only .md or .mdh files
        /// Excludes files with a name starting with "_", with the name "index" or "default".
        /// By default includes only the files directly inside the folder, not in subfolders
        /// 
        /// Syntax: CategsFromFolder folderName topFolderOnly(true*/false or 1/0)
        ///
        /// Examples: 
        /// CategsFromFolder folderName
        /// CategsFromFolder folderName false
        /// </summary>
        /// <param name="currentFile"></param>
        /// <param name="parameters"></param>
        /// <returns>An array of strings with tag names in files, that can be used in Liquid tags</MIISFile></returns>
        object IFMSource.GetValue(MIISFile currentFile, params string[] parameters)
        {
            //Expects these parameters (* indicates the default value):
            //- Folder path relative to the current file or to the root of the site
            //- Top folder files only (true*/false, could be 1*/0) 

            //Check if there's a folder specified
            if (parameters.Length == 0)
                throw new Exception("Folder not specified!!");
            string folderPath = FilesEnumeratorHelper.GetFolderAbsPathFromName(parameters[0]);

            //Include top folder only or all subfolders (second parameter)
            bool topOnly = true;
            try
            {
                string sTopOnly = parameters[1].ToLowerInvariant();
                topOnly = topOnly = FilesEnumeratorHelper.IsTruthy(sTopOnly);   //If the second parameter is "false" or "0", then use topOnly files
            }
            catch { }

            string cacheKey = folderPath + "_categs" + "_" + topOnly;
            //Check if tags are already cached
            var categs = HttpRuntime.Cache[cacheKey];

            if (categs == null)    //Get tags from disk
            {
                //Get al files in the folder (and subfolders if indicated), without specific ordering (we don't need it and it'll save some processing time)
                IEnumerable<MarkdownFile> allFiles = FilesEnumeratorHelper.GetAllFilesFromFolder(folderPath, topOnly);

                //Filter only those that are published
                var publishedFilesProxies = FilesEnumeratorHelper.OnlyPublished(allFiles);

                //Get all the different categories in the published files (if any)
                HashSet<string> hCategs = new HashSet<string>();
                foreach (MIISFile mf in publishedFilesProxies)
                {
                    hCategs.UnionWith(mf.Categories);
                }

                //Get the number of files in each category
                categs = from c in hCategs
                          select new
                          {
                              name = c,
                              count = (from f in publishedFilesProxies
                                       where f.Categories.Contains<string>(c)
                                       select f).Count()
                          };

                //FILE CACHING
                FilesEnumeratorHelper.AddCacheDependencies(currentFile, folderPath, allFiles);

                //Add categories to cache depending on the folder and the time until the next published file
                FilesEnumeratorHelper.CacheResults(folderPath, cacheKey,
                                                   FilesEnumeratorHelper.NumSecondsToNextFilePubDate(allFiles),
                                                   categs);
            }

            return categs;

        }
    }
}
