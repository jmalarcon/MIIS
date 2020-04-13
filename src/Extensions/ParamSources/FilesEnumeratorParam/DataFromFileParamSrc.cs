using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MIISFilesEnumeratorFMS;

namespace MIISHandler.FMSources
{
    class DataFromFileParamSrc : IFMSource
    {
        string IFMSource.SourceName => "DataFromFile";

        /// <summary>
        /// Returns a file proxy to access the data of the specified file to be used in a content
        /// Very useful for loading a specific file when we know it's name
        /// </summary>
        /// <param name="currentFile"></param>
        /// <param name="parameters"></param>
        /// <returns>A MIISFile object to be used in a file content with liquid tags</returns>
        object IFMSource.GetValue(MIISFile currentFile, params string[] parameters)
        {
            if (parameters.Length == 0)
                throw new Exception("Data file not specified!!");

            string filePath = FilesEnumeratorHelper.GetFolderAbsPathFromName(parameters[0]);

            if (!File.Exists(filePath))
                throw new Exception($"The file {filePath} does not exist!!");

            //Add cache dependency on the loaded file
            currentFile.AddFileDependency(filePath);

            //Return the file data proxy
            return new MIISFile(new MarkdownFile(filePath));
        }
    }
}
