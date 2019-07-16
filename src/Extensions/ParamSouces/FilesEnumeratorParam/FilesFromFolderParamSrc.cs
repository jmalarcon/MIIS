using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FilesEnumeratorParam;

namespace MIISHandler.FMSources
{
    public class FilesFromFolderParamSrc : IFMSource
    {
        string IFMSource.SourceName => "FilesFromFolder";

        /// <summary>
        /// Returns all the files in the specified folder. 
        /// Includes only .md or .mdh files
        /// Excludes files with a name starting with "_", with the name "index" or "default".
        /// 
        /// Syntax: FilesFromFolder folderName sortorder (asc/desc) topFolderOnly (true/false)
        ///
        /// Examples: 
        /// FilesFromFolder folderName
        /// FilesFromFolder folderName asc
        /// FilesFromFolder folderName asc false
        /// </summary>
        /// <param name="currentFile"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        object IFMSource.GetValue(MIISFile currentFile, params string[] parameters)
        {
            //Expects these parameters (* indicates the default value):
            //- Folder path relative to the current file or to the root of the site
            //- Sort direction (desc*, asc)
            //- Top folder files only (true*, false)

            //Check if there's a folder specified
            if (parameters.Length == 0)
                throw new Exception("Folder not specified!!");
            string folder = FilesEnumerator.GetFolderAbsPathFromName(parameters[0]);

            //Use the correct sort order, if any (second parameter)
            SortDirection sd = SortDirection.desc;    //Default value
            try
            {
                sd = (SortDirection)Enum.Parse(typeof(SortDirection), parameters[1]);
            }
            catch { }

            //Include top folder only or all subfolders
            bool topOnly = true;
            try
            {
                topOnly = parameters[2].ToLower() != "false";
            }
            catch { }

            //Get al files in the folder (and subfolders if indicated)
            IEnumerable<MarkdownFile> allFiles = FilesEnumerator.GetAllFilesFromFolder(folder, topOnly);

            var allFilesSorted = (sd == SortDirection.desc) ? allFiles.OrderByDescending<MarkdownFile, DateTime>(f => f.Date) : allFiles.OrderBy<MarkdownFile, DateTime>(f => f.Date);

            //Filter only those that are published
            var allFilesProxies = from file in allFilesSorted
                                  where file.IsPublished
                                  select new MIISFile(file);

            //TODO: Establish caching options in the initial file and a custom cache for this component with the result

            return new FileList(allFilesProxies.ToList<MIISFile>());
        }
    }
}
