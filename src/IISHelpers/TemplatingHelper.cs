using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace IISHelpers
{
    internal static class TemplatingHelper
    {
        internal static readonly string PLACEHOLDER_PREFIX = "{{";   //Placeholders' prefix
        internal static readonly string PLACEHOLDER_SUFFIX = "}}";    //Placeholders' suffix
        //Placeholders' name pattern (includes "/" for paths, "." for file names
        internal static readonly string PLACEHOLDER_NAME_REGEX = @"[0-9A-Z\/\.\-_]+?";


        //Extension Method for strings that does a Case-Insensitive Replace()
        //Takes into account replacement strings with $x that would be mistaken for RegExp substitutions
        internal static string ReplacePlaceHolder(string originalContent, string placeholderName, string newValue)
        {
            if (string.IsNullOrEmpty(originalContent) || string.IsNullOrEmpty(placeholderName))
                return originalContent;

            if (string.IsNullOrEmpty(newValue))
                newValue = string.Empty;

            return Regex.Replace(originalContent,
                GetPlaceholderRegexString(placeholderName),
                newValue.Replace("$", "$$"),    //Prevents any substitution elements (such as $0, $$... https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference#substitutions) in the new value
                RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Builds the appropriate regular expression to search for an specific placeholder name using the correct prefix and suffix
        /// </summary>
        /// <param name="placeholderName">The name of the placeholder</param>
        /// <returns></returns>
        private static string GetPlaceholderRegexString(string placeholderName, string phPrefix = "")
        {
            if (placeholderName != PLACEHOLDER_NAME_REGEX)
                placeholderName = Regex.Escape(placeholderName);

            return Regex.Escape(PLACEHOLDER_PREFIX) + @"\s*?" + Regex.Escape(phPrefix) + placeholderName + @"\s*?" + Regex.Escape(PLACEHOLDER_SUFFIX);
        }

        /// <summary>
        /// Gets all the placeholders in the content, as regular expression matches
        /// </summary>
        /// <param name="content"></param>
        /// <param name="name">Optional. The name of a specific palceholder to search. If not provided returns all the placeholders present in the content</param>
        /// <param name="phPrefix">The prefix for all the reutrned placeholders</param>
        /// <returns>A collection of RegExp matches with the placeholders in a content</returns>
        internal static MatchCollection GetAllPlaceHolderMatches(string content, string name = "", string phPrefix = "")
        {
            string phName = string.IsNullOrEmpty(name) ? PLACEHOLDER_NAME_REGEX : Regex.Escape(name);
            Regex rePlaceholders = new Regex(GetPlaceholderRegexString(phName, phPrefix), RegexOptions.IgnoreCase);
            return rePlaceholders.Matches(content);
        }

        /// <summary>
        /// Gets all the placeholders in the content, as a string array
        /// </summary>
        /// <param name="content"></param>
        /// <returns>An array of strings with the name, without duplicates</returns>
        internal static string[] GetAllPlaceHolderNames(string content, string name = "", string phPrefix = "")
        {
            MatchCollection matches = GetAllPlaceHolderMatches(content, name, phPrefix);

            string[] names = new string[matches.Count];

            for (int i =0;  i<matches.Count; i++)
            {
                Match field = matches[i];
                names[i] = GetFieldName(field.Value);
            }
            return names.Distinct<string>().ToArray();
        }

        /// <summary>
        /// Checks if the specified placeholder is present in the content
        /// </summary>
        /// <param name="content">The content to test</param>
        /// <param name="placeholderName">The name of the placeholder (without wrappers)</param>
        /// <returns></returns>
        internal static bool IsPlaceHolderPresent(string content, string placeholderName)
        {
            Regex rePlaceHolder = new Regex(GetPlaceholderRegexString(placeholderName), RegexOptions.IgnoreCase);
            return rePlaceHolder.IsMatch(content);
        }

        /// <summary>
        /// Given a placeholder found in a content it extracts it's name without prefix and suffix and in lowercase.
        /// It takes for granted that it's a correct placeholder and won't check it (it's only used internally after a match)
        /// </summary>
        /// <param name="placeholder">The placeholder that was found</param>
        /// <returns>The name of the placeholder in lowercase</returns>
        internal static string GetFieldName(string placeholder)
        {
            return placeholder.Substring(PLACEHOLDER_PREFIX.Length, placeholder.Length - (PLACEHOLDER_PREFIX.Length + PLACEHOLDER_SUFFIX.Length)).Trim().ToLowerInvariant();
        }

        /// <summary>
        /// Given a placeholder name, returns the full placeholder, using the defined prefix and suffix
        /// </summary>
        /// <param name="name">The placeholder name</param>
        /// <returns></returns>
        internal static string GetPlaceholderName(string name)
        {
            return PLACEHOLDER_PREFIX + name + PLACEHOLDER_SUFFIX;
        }

        /// <summary>
        /// Gets the first paragraph in a content string. 
        /// If it's HTML, first strips all the HTML tags
        /// If it's plain text (i.e: Markdown), then gets the text up to the first new line.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        internal static string GetFirstParagraphText(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return "";

            /*
             * This is a last resort to get content from HTML. I've should have used a parser but since using this 
             * should be a last resort and the result is not so important, I'm using quick and dirty (and prone to failure) regular expressions
            */

            //Remove all the header tags
            string plainContent = Regex.Replace(content, @"<(h\d)[^>]*?>.*?<\/\1>", string.Empty, RegexOptions.Singleline, TimeSpan.FromSeconds(1));

            //Strip HTML
            plainContent = Regex.Replace(plainContent, @"<.*?>", string.Empty, RegexOptions.None, TimeSpan.FromSeconds(1));

            return new Regex(@"(.*\S.*)(\r\n|\r|\n|$)", RegexOptions.Multiline).Matches(plainContent)[0].Groups[1].Captures[0].Value.Trim(); ;
        }
    }
}
