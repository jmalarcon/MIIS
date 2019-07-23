using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FilesEnumeratorParam;

namespace MIISHandler.FMSources
{
    public class TagsFolderParamSrc : IFMSource
    {
        string IFMSource.SourceName => "TagsFromFolder";

        /// <summary>
        /// Returns all the distinct tags available in the files of the specified folder order by tag name (ascending).
        /// Includes only .md or .mdh files
        /// Excludes files with a name starting with "_", with the name "index" or "default".
        /// By default includes only the files directly inside the folder, not in subfolders
        /// 
        /// Syntax: TagsFromFolder folderName topFolderOnly(true*/false or 1/0)
        ///
        /// Examples: 
        /// FilesFromFolder folderName
        /// FilesFromFolder folderName false
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
                string sTopOnly = parameters[1].ToLower();
                topOnly = topOnly = FilesEnumeratorHelper.IsTruthy(sTopOnly);   //If the second parameter is "false" or "0", then use topOnly files
            }
            catch { }


            //Get al files in the folder (and subfolders if indicated), without specific ordering (we don't need it and it'll save some processing time)
            IEnumerable<MarkdownFile> allFiles = FilesEnumeratorHelper.GetAllFilesFromFolder(folderPath, topOnly);

            //Filter only those that are published
            var publishedFilesProxies = FilesEnumeratorHelper.OnlyPublished(allFiles);

            //Get all the tags in the published files (if any)
            HashSet<string> hTags = new HashSet<string>();
            //int max = publishedFilesProxies.Count() - 1;
            //for(int i = 0; i<=max; i++)
            foreach (MIISFile mf in publishedFilesProxies)
            {
                hTags.UnionWith(mf.Tags);
            }

            //FILE CACHING
            FilesEnumeratorHelper.AddCacheDependencies(currentFile, folderPath, allFiles);

            //Return tags
            return hTags.ToArray<string>();

        }
    }
}
