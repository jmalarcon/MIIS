using System;
using System.Text.RegularExpressions;

namespace IISHelpers
{
    internal static class TemplatingHelper
    {
        //The regular expression to find fields in templates
        internal static string FIELD_PREFIX = "{{";
        internal static string FIELD_SUFIX = "}}";
        internal static Regex REGEXFIELDS_PATTERN = new Regex(Regex.Escape(FIELD_PREFIX) + @"\s*?[0-9A-Z\*\-_]+?\s*?" + Regex.Escape(FIELD_SUFIX), RegexOptions.IgnoreCase);

        //Extension Method for strings that does a Case-Insensitive Replace()
        //Takes into account replacement strings with $x that would be mistaken for RegExp substituions
        internal static string ReplacePlaceHolder(string originalContent, string placeholderName, string newValue)
        {
            return Regex.Replace(originalContent,
                Regex.Escape(FIELD_PREFIX) + "\\s*?" + Regex.Escape(placeholderName) + "\\s*?" + Regex.Escape(FIELD_SUFIX),
                Regex.Replace(newValue, "\\$[0-9]+", @"$$$0"),
                RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Given a placeholder found in a content it extracts it's name without prefix and suffix and in lowercase.
        /// It takes for granted that it's a correct placeholder and won't check it (it's only used internally after a match)
        /// </summary>
        /// <param name="placeholder">The placeholder that was found</param>
        /// <returns>The name of the placeholder in lowercase</returns>
        internal static string GetFieldName(string placeholder)
        {
            return placeholder.Substring(FIELD_PREFIX.Length, placeholder.Length - (FIELD_PREFIX.Length + FIELD_SUFIX.Length)).Trim().ToLower();
        }
    }
}
