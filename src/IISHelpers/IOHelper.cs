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
            string cachedContent = HttpRuntime.Cache[filePath] as string;
            if (string.IsNullOrEmpty(cachedContent))
            {
                string content = ReadTextFromFile(filePath);    //Read file contents from disk
                HttpRuntime.Cache.Insert(filePath, content, new CacheDependency(filePath)); //Add result to cache with dependency on the file
                return content; //Return content
            }
            else
                return cachedContent;   //Return directly from cache
        }
    }
}
