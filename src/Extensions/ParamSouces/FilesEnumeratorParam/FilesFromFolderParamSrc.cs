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
        /// Returns all the files in the specified folder order by descending date.
        /// Includes only .md or .mdh files
        /// Excludes files with a name starting with "_", with the name "index" or "default".
        /// By default includes only the files directly inside the folder, not in subfolders
        /// 
        /// Syntax: FilesFromFolder folderName topFolderOnly(true*/false or 1/0) sortDirection (desc*/asc)
        ///
        /// Examples: 
        /// FilesFromFolder folderName
        /// FilesFromFolder folderName false
        /// FilesFromFolder folderName true asc
        /// </summary>
        /// <param name="currentFile"></param>
        /// <param name="parameters"></param>
        /// <returns>A List of file information objects that can be used in Liquid tags</MIISFile></returns>
        object IFMSource.GetValue(MIISFile currentFile, params string[] parameters)
        {
            //Expects these parameters (* indicates the default value):
            //- Folder path relative to the current file or to the root of the site
            //- Top folder files only (true*/false, could be 1*/0) 

            //Check if there's a folder specified
            if (parameters.Length == 0)
                throw new Exception("Folder not specified!!");
            string folder = FilesEnumerator.GetFolderAbsPathFromName(parameters[0]);

            //Include top folder only or all subfolders (second parameter)
            bool topOnly = true;
            try
            {
                string sTopOnly = parameters[1].ToLower();
                topOnly = !(sTopOnly == "false" || sTopOnly == "0"); //If the second parameter is "false" or "0", then use topOnly files
            }
            catch { }

            //Use the correct sort order, if any (third parameter)
            SortDirection sd = SortDirection.desc;    //Default value
            try
            {
                sd = (SortDirection)Enum.Parse(typeof(SortDirection), parameters[2]);
            }
            catch { }

            //Get al files in the folder (and subfolders if indicated), ordered by date desc
            IEnumerable<MarkdownFile> allFiles = FilesEnumerator.GetAllFilesFromFolder(folder, topOnly, sd);
            
            //TODO: Establish caching options in the initial file and a custom cache for this component with the result

            //Filter only those that are published
            var publishedFilesProxies = from file in allFiles
                                  where file.IsPublished
                                  select new MIISFile(file);

            //return new FileList(publishedFilesProxies.ToList<MIISFile>());
            return publishedFilesProxies.ToList<MIISFile>();
        }
    }
}
