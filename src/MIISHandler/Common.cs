using System;
using IISHelpers;

namespace MIISHandler
{
    public static class Common
    {
        public const string WEB_CONFIG_PARAM_PREFIX = "MIIS:"; //THe prefix to use to search for parameters in web.config

        public static string GetFieldValueFromFM(string name, MarkdownFile md, string defValue = "")
        {
            if (md != null) //If there's a file, possibly with a Front Matter
            {
                //Retrieve from the front matter...
                string val = md.FrontMatter[name];
                if (!string.IsNullOrEmpty(val))
                    return val;
                else
                    return defValue;    //Return defValue if field is not available
            }
            else
            {
                return defValue;    //Return defValue if there's not MD file to process
            }
        }

        /// <summary>
        /// Returns the value, if any, for a specified field name. It takes the value from the FrontMatter first, and if it's not there, tries to read it from the current Web.config.
        /// In web.config it first tries to read them prefixed with "MIIS_" to prevent collision with other products, and then without the prefix.
        /// If it's not present neither in the Front Matter nor the Web.config, returns the default value.
        /// </summary>
        /// <param name="name">The name of the field to retrieve</param>
        /// <param name="md">An optional Markdown file to check in its front matter</param>
        /// <param name="defValue">The default value to return if it's not present</param>
        /// <returns></returns>
        public static string GetFieldValue(string name, MarkdownFile md = null, string defValue = "")
        {
            if (md != null) //If there's a file, possibly with a Front Matter
            {
                //Retrieve from the front matter...
                string val = md.FrontMatter[name];
                if (!string.IsNullOrEmpty(val))
                    return val;
            }

            return GetFieldValueFromConfig(name, defValue);
        }

        public static string GetFieldValueFromConfig(string name, string defValue)
        {
            //Retrieve from Web.config using the app-specific prefix or without it if it's not present
            return WebHelper.GetParamValue(WEB_CONFIG_PARAM_PREFIX + name, WebHelper.GetParamValue(name, defValue));
        }
    }
}