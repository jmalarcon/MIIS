using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;
using MIISFilesEnumeratorFMS;

namespace MIISHandler.FMSources
{
    public class TagsFolderParamSrc : IFMSource
    {
        string IFMSource.SourceName => "TagsFromFolder";

        /// <summary>
        /// Returns an array with all the distinct tags available in the files of the specified folder ordered by tag name (ascending).
        /// Includes only .md or .mdh files
        /// Excludes files with a name starting with "_", with the name "index" or "default".
        /// By default includes only the files directly inside the folder, not in subfolders
        /// 
        /// Syntax: TagsFromFolder folderName topFolderOnly(true*/false or 1/0)
        ///
        /// Examples: 
        /// TagsFromFolder folderName
        /// TagsFromFolder folderName false
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

            string cacheKey = folderPath + "_tags" + "_" + topOnly;
            //Check if tags are already cached
            var tags = HttpRuntime.Cache[cacheKey];

            if (tags == null)    //Get tags from disk
            {
                //Get al files in the folder (and subfolders if indicated), without specific ordering (we don't need it and it'll save some processing time)
                IEnumerable<MarkdownFile> allFiles = FilesEnumeratorHelper.GetAllFilesFromFolder(folderPath, topOnly);

                //Filter only those that are published
                var publishedFilesProxies = FilesEnumeratorHelper.OnlyPublished(allFiles);

                //Get all the tags in the published files (if any)
                HashSet<string> hTags = new HashSet<string>();
                foreach (MIISFile mf in publishedFilesProxies)
                {
                    hTags.UnionWith(mf.Tags);
                }

                //Get the number of files in each tag
                tags = from t in hTags
                         select new
                         {
                             name = t,
                             count = (from f in publishedFilesProxies
                                      where f.Tags.Contains<string>(t)
                                      select f).Count()
                         };

                //FILE CACHING
                FilesEnumeratorHelper.AddCacheDependencies(currentFile, folderPath, allFiles);

                //Add tags to cache depending on the folder and the time until the next published file
                FilesEnumeratorHelper.CacheResults(folderPath, cacheKey,
                                                   FilesEnumeratorHelper.NumSecondsToNextFilePubDate(allFiles), 
                                                   tags);
            }

            return tags;

        }
    }
}
