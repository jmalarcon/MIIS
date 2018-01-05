using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace IISHelpers
{
    internal static class TemplatingHelper
    {
        //The regular expression to find fields in templates
        internal static string FIELD_PREFIX = "{{";
        internal static string FIELD_SUFIX = "}}";
        internal static string FIELD_NAME_REGEX = @"\s*?[0-9A-Z\*\$\.\-_]+?\s*?";


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
        /// Gets all the placeholders in the content, as regular expression matches
        /// </summary>
        /// <param name="content"></param>
        /// <returns>A collection of RegExp matches with the placeholders in a content</returns>
        internal static MatchCollection GetAllPlaceHolderMatches(string content)
        {
            Regex REGEXFIELDS_PATTERN = new Regex(Regex.Escape(FIELD_PREFIX) + FIELD_NAME_REGEX + Regex.Escape(FIELD_SUFIX), RegexOptions.IgnoreCase);
            return REGEXFIELDS_PATTERN.Matches(content);
        }

        /// <summary>
        /// Gets all the placeholders in the content, as a string array
        /// </summary>
        /// <param name="content"></param>
        /// <returns>An array of strings with the name, without duplicates</returns>
        internal static string[] GetAllPlaceHolders(string content)
        {
            Regex REGEXFIELDS_PATTERN = new Regex(Regex.Escape(FIELD_PREFIX) + FIELD_NAME_REGEX + Regex.Escape(FIELD_SUFIX), RegexOptions.IgnoreCase);

            MatchCollection matches = REGEXFIELDS_PATTERN.Matches(content);

            string[] names = new string[matches.Count];

            for (int i =0;  i<matches.Count; i++)
            {
                Match field = matches[i];
                names[i] = GetFieldName(field.Value);
            }
            return names.Distinct<string>().ToArray();
        }

        internal static bool IsPlaceHolderPresent(string content, string placeholderName)
        {
            //Regex.Match()
            return true;
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
