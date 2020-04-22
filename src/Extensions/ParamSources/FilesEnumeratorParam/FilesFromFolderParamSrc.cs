using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Web;
using MIISFilesEnumeratorFMS;

namespace MIISHandler.FMSources
{
    public class FilesFromFolderParamSrc : IFMSource, IQueryStringDependent
    {
        //Declared as static string[] to reuse them in other FM Sources in this assembly
        internal static readonly string[] QSParams = new string[] { "tag", "categ" };
        string IFMSource.SourceName => "FilesFromFolder";

        string[] IQueryStringDependent.GetCachingQueryStringFields()
        {
            return QSParams;
        }

        /// <summary>
        /// Returns the files in the specified folder ordered by descending date by default.
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
        /// 
        /// It can automatically detect the tag or categ params to filter by tag or category if available
        /// </summary>
        /// <param name="currentFile"></param>
        /// <param name="parameters"></param>
        /// <returns>A List of file information objects that can be used in Liquid tags</MIISFile></returns>
        object IFMSource.GetValue(MIISFile currentFile, params string[] parameters)
        {
            //Get all published files with the current parameters
            return GetFilesFromParameters(currentFile, parameters).ToList<MIISFile>();
        }

        //Ugly method to have in one place all the code common to various FM Sources that are in this assembly
        //DRY, you know ;-)
        internal static IEnumerable<MIISFile> GetFilesFromParameters(MIISFile currentFile, params string[] parameters)
        {
            //Expects these parameters (* indicates the default value):
            //- Folder path relative to the current file or to the root of the site
            //- Top folder files only (true*/false, could be 1*/0) 

            //First parameter: folder to process
            //Check if there's a folder specified
            if (parameters.Length == 0)
                throw new Exception("Folder not specified!!");
            string folderPath = FilesEnumeratorHelper.GetFolderAbsPathFromName(parameters[0]);

            //Second parameter: include files in subfolders
            //Include top folder only or all subfolders
            bool topOnly = true;
            try
            {
                string sTopOnly = parameters[1].ToLowerInvariant();
                topOnly = FilesEnumeratorHelper.IsTruthy(sTopOnly); //If the second parameter is "false" or "0", then use topOnly files
            }
            catch { }

            //Third parameter: include default files (index, default)
            bool includeDefFiles = false;
            try
            {
                string sincludeDefFiles = parameters[2].ToLowerInvariant();
                includeDefFiles = FilesEnumeratorHelper.IsTruthy(sincludeDefFiles);
            }
            catch { }

            //Get al files in the folder (and subfolders if indicated), ordered by date desc
            IEnumerable<MarkdownFile> allFiles = FilesEnumeratorHelper.GetAllFilesFromFolder(folderPath, topOnly, includeDefFiles);

            //File caching dependencies
            FilesEnumeratorHelper.AddCacheDependencies(currentFile, folderPath, allFiles);

            //Filter only those that are published
            IEnumerable<MIISFile> publishedFilesProxies = FilesEnumeratorHelper.OnlyPublished(allFiles);
            int numPublishedFiles = publishedFilesProxies.Count();

            //Filter by tag or category from QueryString params
            NameValueCollection qs = HttpContext.Current.Request.QueryString;
            if (!string.IsNullOrWhiteSpace(qs["tag"]))  //More specific, takes precedence
            {
                string tag = qs["tag"].ToLowerInvariant();
                publishedFilesProxies = from file in publishedFilesProxies
                                        where file.Tags.Contains<string>(tag)
                                        select file;
            }
            else if (!string.IsNullOrWhiteSpace(qs["categ"]))
            {
                string categ = qs["categ"].ToLowerInvariant();
                publishedFilesProxies = from file in publishedFilesProxies
                                        where file.Categories.Contains<string>(categ)
                                        select file;
            }

            return publishedFilesProxies;
        }
    }
}
