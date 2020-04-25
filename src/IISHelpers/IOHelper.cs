using System;
using System.IO;
using System.Web;
using System.Web.Caching;

namespace IISHelpers
{
    internal static class IOHelper
    {
        /// <summary>
        /// Tries to read the requested file from disk and returns its contents
        /// </summary>
        /// <param name="requestPath">Path to the file</param>
        /// <returns>The text contents of the file</returns>
        /// <exception cref="System.Security.SecurityException">Thrown if the app has no read access to the requested file</exception>
        /// <exception cref="System.IO.FileNotFoundException">Thrown when the requested file does not exist</exception>
        internal static string ReadTextFromFile(string filePath)
        {
            //The route of the file we need to process
            using (StreamReader srMD = new StreamReader(filePath))
            {
                return srMD.ReadToEnd(); //Text file contents
            }
        }

        /// <summary>
        /// Reads a file from cache if available. If not, reads it from disk.
        /// If read from disk it adds the results to the cache with a dependency on the file 
        /// so that, if the file changes, the cache is immediately invalidated and the new changes read from disk.
        /// </summary>
        /// <param name="requestPath">Path to the file</param>
        /// <returns>The text contents of the file</returns>
        /// <exception cref="System.Security.SecurityException">Thrown if the app has no read access to the requested file</exception>
        /// <exception cref="System.IO.FileNotFoundException">Thrown when the requested file does not exist</exception>
        internal static string ReadTextFromFileWithCaching(string filePath)
        {
            string cachedContent = HttpRuntime.Cache[filePath + "_content"] as string;
            if (string.IsNullOrEmpty(cachedContent))
            {
                string content = ReadTextFromFile(filePath);    //Read file contents from disk
                HttpRuntime.Cache.Insert(filePath + "_content", content, new CacheDependency(filePath)); //Add result to cache with dependency on the file
                return content; //Return content
            }
            else
                return cachedContent;   //Return directly from cache
        }

        /// <summary>
        /// Removes the file extension from a file path (can be physical or a URL)
        /// </summary>
        /// <param name="path">string with the path</param>
        /// <returns>The path without anything from the last point onward unless there's a slash after it</returns>
        public static string RemoveFileExtension(string path)
        {
            int dotPos = path.LastIndexOf('.');
            //If there's no final point
            if (dotPos == -1)
                return path;    //Return unmodified path
            //If there's a last point, check that it's not before a folder separator
            int slashPos = path.LastIndexOf('\\');  //Position of folder separator (physical path)
            if (slashPos == -1) slashPos = path.LastIndexOf('/');   //Try with virtual url path or phys path in UNIX systems
            if (slashPos > dotPos) //If the slash is afetr the dot, then is not a file extension but a dot in a folder name
                return path;    //return full path
            //If we reach this point then, remove everyting after the point position (included)
            return path.Substring(0, dotPos);
        }

        /// <summary>
        /// Returns the whole path without the file at the end
        /// </summary>
        /// <param name="filePath">The path to a file</param>
        /// <returns></returns>
        public static string GetContainingFolderPath(string filePath)
        {
            FileInfo fi = new FileInfo(filePath);
            return fi.DirectoryName;
        }
    }
}
