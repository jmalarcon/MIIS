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

        private string YAML_PROP_RE_STRING = @"^[\s\t]*?{{propname}}\:(.+)$";

        //The YAML to be processed
        private string yaml;

        /// <summary>
        /// This constructor receives the front matter that we're going to use to search for properties and their values
        /// </summary>
        /// <param name="frontmatter"></param>
        public SimpleYAMLParser(string frontmatter)
        {
            yaml = frontmatter;
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
                Regex re = new Regex(YAML_PROP_RE_STRING.Replace("{{propname}}", propertyName), RegexOptions.Multiline | RegexOptions.IgnoreCase);
                Match property = re.Match(yaml);
                if (property == null)
                    return string.Empty;
                else
                    return property.Groups[1].Value.Trim();
            }
        }

    }
}