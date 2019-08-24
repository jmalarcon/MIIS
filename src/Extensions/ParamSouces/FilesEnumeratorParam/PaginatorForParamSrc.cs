using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using FilesEnumeratorParam;

namespace MIISHandler.FMSources
{
    //This class works exactly like FilesFromFolderParamSrc but returning a Paginator objects instead 
    //and only the files for the current page
    public class PaginatorForParamSrc : IFMSource, IQueryStringDependent
    {
        string IFMSource.SourceName => "PaginatorForFolder";

        string[] IQueryStringDependent.GetCachingQueryStringFields()
        {
            return FilesFromFolderParamSrc.QSParams.Concat<string>(
                    new string[] { "page" } //Extra query string names used apart from the default ones
                ).ToArray<string>();
        }

        /// <summary>
        /// Returns a paginator for the files in the specified folder, ordered by descending date by default.
        /// Includes only .md or .mdh files
        /// Excludes files with a name starting with "_", with the name "index" or "default".
        /// By default includes only the files directly inside the folder, not in subfolders
        /// 
        /// Syntax: PaginatorFor folderName topFolderOnly(true*/false or 1/0) sortDirection (desc*/asc)
        ///
        /// Examples: 
        /// PaginatorFor folderName
        /// PaginatorFor folderName false
        /// PaginatorFor folderName true asc
        /// 
        /// It can automatically detect the tag or categ params to filter by tag or category if available
        /// </summary>
        /// <param name="currentFile"></param>
        /// <param name="parameters"></param>
        /// <returns>A List of file information objects that can be used in Liquid tags</MIISFile></returns>
        object IFMSource.GetValue(MIISFile currentFile, params string[] parameters)
        {
            //Get all published files with the current parameters
            var currentFiles = FilesFromFolderParamSrc.GetFilesFromParameters(currentFile, parameters);

            //Get current page from QS
            NameValueCollection qs = HttpContext.Current.Request.QueryString;
            int page = 1;
            if (!string.IsNullOrWhiteSpace(qs["page"]))
            {
                _ = int.TryParse(qs["page"], out page);
            }

            //Get current specified page size (10 by deafult)
            int PageSize = 0;
            var valPageSize = currentFile["paginate"];
            if (valPageSize != null)
            {
                _ = int.TryParse(valPageSize.ToString(), out PageSize);
            }

            //Define the paginator object
            return new Paginator(currentFiles, page, PageSize);
        }
    }
}
