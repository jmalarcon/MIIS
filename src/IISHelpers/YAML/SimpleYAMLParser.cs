using System;
using System.Text.RegularExpressions;

namespace IISHelpers.YAML
{
    /// <summary>
    /// Quick & Dirty class to read *basic* YAML with regular expressions.
    /// It only allows for simple properties in the form:
    /// 
    /// propertyName: property value
    /// 
    /// one per line.
    /// 
    /// Doesn't support lists, folded or wrapped text. Just simple properties
    /// Property names are case-insensitive
    /// It allows for tabs and spaces in front of the property name (YAML only allows for spaces, but this way is more forgiving)
    /// Values are read as-is. It means that if you wrap them between double quotes, for example, they won't be discarded.
    /// You can make use of comments with #, but always in their own lines. If you add a comment at the end of a line that contains a property, it's included as part of the value for that property
    /// YAML must be delimited by three hyphens (---) at the beggining and the end. No support for three dots (...)
    /// If a property is specified more than once, only the first occurrence is considered
    /// </summary>
    public class SimpleYAMLParser
    {
        //Regexp to detect and extract Front-Matter
        //It allows more than 3 dashed to be used to delimit the Front-Matter (the YAML spec requires exactly 3 dashes, but I like to allow more freedom on this, so 3 or more in a line are allowed)
        //It takes into account the different EOL for Windows (\r\n), Mac (\r) or UNIX (\n)
        public static readonly Regex FRONT_MATTER_RE = new Regex(@"^-{3,}(.*?)-{3,}\s*?(\r\n|\r|\n|$)", RegexOptions.Singleline);
        public static readonly string EMPTY_FRONT_MATTER = "---\r\n---";

        //The YAML Front-Matter to be processed (including the delimiters)
        private string yaml;

        /// <summary>
        /// This constructor receives the front matter that we're going to use to search for properties and their values
        /// </summary>
        /// <param name="frontmatter"></param>
        public SimpleYAMLParser(string frontmatter)
        {
            yaml = frontmatter;
        }

#region Front-Matter manipulation

        /// <summary>
        /// Extracts the Front-Matter from a content and returns it
        /// It always include the "---" delimiters
        /// </summary>
        /// <param name="content">The Front-Matter if present or an empty Front-Matter if not</param>
        public static string GetFrontMatterFromContent(string content)
        {
            Match fm = FRONT_MATTER_RE.Match(content);
            if (fm.Length > 0) //If there's front matter available
            {
                return fm.Groups[0].Value;
            }
            return EMPTY_FRONT_MATTER;
        }

        /// <summary>
        /// Removes the front matter, if any, from the passed content string
        /// and removes the extra empty lines at the beginning
        /// returning the content without the Front-Matter
        /// </summary>
        /// <param name="content">The content where the Front-Matter is present and we want it to be removed</param>
        /// <returns>The original content without the Front-Matter</returns>
        public static string RemoveFrontMatterFromContent(string content)
        {
            return SimpleYAMLParser.FRONT_MATTER_RE.Replace(content, "").TrimStart('\r', '\n');
        }

#endregion

        /// <summary>
        /// Builds the appropriate regular expression to search for an specific placeholder name using the correct prefix and suffix
        /// </summary>
        /// <param name="propName">The name of the placeholder</param>
        /// <returns></returns>
        private static string GetPropertyRegexString(string propName)
        {
            return @"^[\s\t]*?" + Regex.Escape(propName) + @"\:(.+)$";
        }

        /// <summary>
        /// Indexer to return the value of a property from the YAML in the front matter
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public string this[string propertyName]
        {
            get
            {
                //Lots of files won't have front matter, so just return an empty value
                if (string.IsNullOrEmpty(yaml))
                    return string.Empty;

                //Search for the property value using regular expressions
                Regex re = new Regex(GetPropertyRegexString(propertyName), RegexOptions.Multiline | RegexOptions.IgnoreCase);
                Match property = re.Match(yaml);
                if (property == null)
                    return string.Empty;
                else
                {
                    //Remove start and end quotes and double quotes from value if present
                    string res = property.Groups[1].Value.Trim();
                    //Check if it's a string enclosed in double quotes
                    if ( (res.StartsWith("\"") && res.EndsWith("\"")) || (res.StartsWith("'") && res.EndsWith("'")) )
                    {
                        //Remove quotes
                        res = res.Trim().Substring(1, res.Length - 2);
                    }
                    return res;
                }
            }
        }
    }
}