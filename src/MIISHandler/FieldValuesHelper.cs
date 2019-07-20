using System;
using System.Reflection;
using System.Collections.Generic;
using IISHelpers;
using MIISHandler.FMSources;

namespace MIISHandler
{
    public static class FieldValuesHelper
    {
        //The prefix to use in order to search for parameters in web.config
        public const string WEB_CONFIG_PARAM_PREFIX = "MIIS:";

        //Keeps the list of classes that can be sources of Front-Matter fields
        //Since it's a read-only collection after the first load, and access it's locked when loaded for the first time, it's safe to use it
        private static Dictionary<string, Type> _FMSources = new Dictionary<string, Type>();

        /// <summary>
        /// This method takes a (previously obtained) class Type and adds it to the list of valid Front-Matter sources to check for a value's name
        /// </summary>
        internal static void AddFrontMatterSource(string sourceName, Type classType)
        {
            //The name of the FM Source can0t be empty or have spaces in it
            if (string.IsNullOrWhiteSpace(sourceName) || sourceName.IndexOf(" ") >= 0 )
                throw new ArgumentException("The Front-Matter Field Source must be a non empty string without spaces", sourceName);

            //Add it to the list of FM sources
            _FMSources.Add(sourceName.ToLower(), classType);
        }

        /// <summary>
        /// Returns the value, if any, for a specified field name. It takes the value from the FrontMatter only.
        /// If it's not present in the Front Matter, it returns the default value.
        /// </summary>
        /// <param name="name">The name of the field to retrieve</param>
        /// <param name="md">An optional Markdown file to check in its front matter</param>
        /// <param name="defValue">The default value to return if it's not present</param>
        /// <returns></returns>
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
        /// Calls the indicated custom Front-Matter field and returns the resulting value/object to be used into documents/templates
        /// </summary>
        /// <param name="sourceName">The name of the FM source to call. Case insensitive.</param>
        /// <param name="srcParams">a list of params in test form</param>
        /// <returns></returns>
        public static object GetFieldValueFromFMSource(string sourceName, MIISFile file, params string[] srcParams)
        {
            sourceName = sourceName.ToLower();
            if (_FMSources.ContainsKey(sourceName))
            {
                //Instantiate a new class of this type
                IFMSource fms = (IFMSource)Activator.CreateInstance(_FMSources[sourceName]);
                return fms.GetValue(file, srcParams);
            }
            else
            {
                throw new Exception($"The custom Front-Matter source '{sourceName}' does not exist!!");
            }
        }

        /// <summary>
        /// Returns the value, if any, for a specified field name. It takes the value from the FrontMatter first, and if it's not there, tries to read it from the current Web.config.
        /// In web.config it first tries to read them prefixed with "MIIS_" to prevent collision with other products, and then without the prefix.
        /// If it's not present neither in the Front Matter nor the Web.config, returns the default value.
        /// </summary>
        /// <param name="name">The name of the field to retrieve. Case insensitive.</param>
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

            //Retrieve from Web.config using the app-specific prefix or without it if it's not present
            return WebHelper.GetParamValue(WEB_CONFIG_PARAM_PREFIX + name, WebHelper.GetParamValue(name, defValue));
        }
    }
}