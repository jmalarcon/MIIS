using System;
using System.IO;

namespace MIISHandler
{
    /// <summary>
    /// Proxy class to an underlying processed file to expose oly a few features through a DotLiquid context
    /// </summary>
    public class MIISFile : DotLiquid.Drop
    {
        //Reference to the internal MD or MDF file
        private MarkdownFile md;
        public  MIISFile(MarkdownFile mdFile)
        {
            md = mdFile;
        }

        /// <summary>
        /// Current file's name
        /// </summary>
        public string FileName
        {
            get
            {
                return md.FileName;
            }
        }

        /// <summary>
        /// Current file's absolute path in disk
        /// </summary>
        public string FilePath
        {
            get
            {
                return md.FilePath;
            }
        }

        /// <summary>
        /// Allows a custom tag to add a new file dependency to the current MD or MDH file caching
        /// </summary>
        /// <param name="filePath">The full path to the file that the current file depends on</param>
        public void AddFileDependency(string filePath)
        {
            //If is a valid file and it's not already added to the dependencies, add it
            if (File.Exists(filePath) && !md.Dependencies.Contains(filePath))
                md.Dependencies.Add(filePath);
        }
    }
}