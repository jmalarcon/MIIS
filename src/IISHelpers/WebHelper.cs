using MIISHandler;
using System;
using System.Configuration;
using System.Web;
using System.Web.Configuration;

namespace IISHelpers
{
    internal static class WebHelper
    {
        /// <summary>
        /// Returns a param from web.config or a default value for it
        /// The defaultValue can be skipped and it will be returned an empty string if it's needed
        /// </summary>
        /// <param name="paramName">The name of the param to search in the configuration file</param>
        /// <param name="defaultvalue">The default value to return if the param is not found. It's optional. If missing it will return an empty string</param>
        /// <returns></returns>
        internal static string GetParamValue(string paramName, string defaultvalue = "")
        {
            string v = WebConfigurationManager.AppSettings[paramName];
            return String.IsNullOrEmpty(v) ? defaultvalue : v.Trim();
        }

        /// <summary>
        /// Returns a param from web.config using the indicated file context, that is, the web.config in the file's folder
        /// or a default value for it if it's not available
        /// The defaultValue can be skipped and it will be returned an empty string if it's needed
        /// </summary>
        /// <param name="paramName">The name of the param to search in the configuration file</param>
        /// <param name="defaultvalue">The default value to return if the param is not found. It's optional. If missing it will return an empty string</param>
        /// <param name="md">The Markdown or MDH file which parameters are going to be processed. It it's null, it'll use the current file context config and will be equivalent to using GetParamValue.
        /// <returns></returns>
        internal static string GetParamValueInContext(string paramName, string defaultvalue = "", MarkdownFile md = null)
        {
            //If it's null or the file is the same that is being processed by the request, then return the normal value
            if (md == null)
                return GetParamValue(paramName, defaultvalue);

            HttpContext ctx = HttpContext.Current;

            if (ctx.Application["MDFilesInOwnContext"] == null || ctx.Application["MDFilesInOwnContext"].ToString().ToLower() != "true")
                return GetParamValue(paramName, defaultvalue);

            String contextDir = GetContainingDir(GetAbsolutePath(md.FilePath));
            if (GetContainingDir(ctx.Request.Url.LocalPath) == contextDir)
                return GetParamValue(paramName, defaultvalue);

            //Load the configuration file for the current file's path
            KeyValueConfigurationCollection currConf = WebConfigurationManager.OpenWebConfiguration(contextDir).AppSettings.Settings;
            string v = currConf[paramName]?.Value;
            return String.IsNullOrEmpty(v) ? defaultvalue : v.Trim();
        }

        /// <summary>
        /// Searchs for virtual paths ("~/") and transform them to absolute paths (relative to the root of the server)
        /// </summary>
        /// <param name="content">The content to transform</param>
        /// <returns></returns>
        internal static string TransformVirtualPaths(string content)
        {
            string absoluteBase = VirtualPathUtility.ToAbsolute("~/");
            content = content.Replace("~/", absoluteBase);
            //Markdig codifies the "~" as "%7E" , so we need to process it this way too
            return content.Replace("%7E/", absoluteBase);
        }

        //Gets current user IP (even if it's forwarded by a proxy
        internal static string GetIPAddress()
        {
            HttpContext ctx = HttpContext.Current;
            string ip = ctx.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (!string.IsNullOrEmpty(ip))
            {
                string[] addresses = ip.Split(',');
                if (addresses.Length != 0)
                {
                    return addresses[0];
                }
            }

            return ctx.Request.ServerVariables["REMOTE_ADDR"];
        }

        /// <summary>
        /// Returns the absolute path (from the web app's root folder) from the physical path of a file
        /// The file should be one inside the app's folder
        /// </summary>
        /// <param name="physicalPath"></param>
        /// <returns></returns>
        public static string GetAbsolutePath(string physicalPath)
        {
            string baseAppPath = HttpContext.Current.Request.PhysicalApplicationPath;
            return physicalPath.Replace(baseAppPath, "/").Replace(@"\", "/");
        }

        /// <summary>
        /// Gets the containing directory for the current file specified by it's VirtualPath
        /// </summary>
        /// <param name="virtPath">The virtual path for the file</param>
        /// <returns>The virtual path for the current file's container directory, with the ending slash</returns>
        public static string GetContainingDir(string virtPath)
        {
            int slashPos = virtPath.LastIndexOf('/');
            //If there's no directory
            if (slashPos == -1)
                return "/";    //Return the base folder
                               //If it's a path ending with a slash, so already a directory
            if (slashPos == virtPath.Length - 1)
                return virtPath;    //Return the original path
                                    //Else, remove the part from the last slash
            return virtPath.Substring(0, slashPos + 1);
        }
    }
}
