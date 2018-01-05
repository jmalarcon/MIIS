using System;
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
    }
}
