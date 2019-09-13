using System;
using System.Linq;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using DotLiquid;

namespace MIISHandler.Filters
{

    //Needed by MIIS to add the correct reference to DotLiquid
    public class WhereFilterFactory : IFilterFactory
    {
        Type IFilterFactory.GetFilterType()
        {
            return typeof(WhereFilter);
        }
    }

    public static class WhereFilter
    {
        /// <summary>
        /// This filter takes an IEnumerable (array, IList...) and filters it to make the specified property match
        /// with the specified value to check. It's smart enough to match several values at once if you pass an array 
        /// or any other enumerable as a value2Check parameter.
        /// It takes into account to that the value of the property is an IEnumerable, and checks if any of them matches.
        /// </summary>
        /// <param name="input">Only an IEnumerable makes sense in this kind of filtering</param>
        /// <param name="propertyName">The name of the property to check for the matching</param>
        /// <param name="value2Check">The value to be checked against. Can be several values if it receives an IEnumerable. Any match of them will be enough.</param>
        /// <returns>A filtered collection in the form of an ArrayList. This will be empty the resulting match does not work for any object.
        /// IMPORTANT: In the case of strings they will be matched CASE INSENSITIVE to maximize the chance of matching</returns>
        public static object Where(IEnumerable input, string propertyName, object value2Check = null)
        {
            ArrayList res = new ArrayList();

            if (value2Check == null)
                value2Check = true;

            foreach (var elt in input)
            {
                object pv = null;

                try //to get the value for the property
                {
                    //Act according to specific cases
                    
                    //Common case: a MIISFile with well-known properties and overwritten indexer --> Go with the indexer
                    if(elt is MIISFile)
                    {
                        pv = (elt as MIISFile)[propertyName];
                    }
                    else if (elt is IIndexable) //A DotLiquid.Drop, also indexable by default (although maybe not overwitten and therefore gets it by reflection too)
                    {
                        pv = (elt as IIndexable)[propertyName];
                    }
                    else if (elt is Hash)   //A DotLiquid.Hash, with an specific method for this
                    {
                        _ = (elt as Hash).TryGetValue(propertyName, out pv);
                    }
                    else  //Any other case
                    {
                        //Just try to get the value of the property (through reflection) using the correct C# name 
                        //independently of the current Naming Convention used (this is C# reflection)
                        Type t = elt.GetType();
                        pv = t.GetProperty(GetCSharpName(propertyName), 
                            BindingFlags.Public | 
                            BindingFlags.Instance | 
                            BindingFlags.IgnoreCase)?.GetValue(elt);    //Property value (ignoring case)
                    }

                }
                catch{} //pv would be null anyways

                //If a valid value is returned
                if (pv != null)
                {
                    //Check equality with the returned value
                    if (checkMatch(pv, value2Check))
                        res.Add(elt);
                }
            }
            return res;
        }

        public static object WithName(IEnumerable input, object value2Check = null)
        {
            return Where(input, 
                Template.NamingConvention is DotLiquid.NamingConventions.RubyNamingConvention ? "file_name_no_ext" : "FileNameNoExt",
                value2Check);
        }

        #region Private helper methods
        //Checks if two values match taking into account several conditions such if the matching value is an IEnumerable...
        private static bool checkMatch(object propertyValue, object value2Check)
        {
            //Check if propertyValye is IEnumerable
            if (propertyValue is IEnumerable && !(propertyValue is string))
            {
                //If the property value is an Array or similar... call itself with each element
                foreach (object val in propertyValue as IEnumerable)
                {
                    //Check if ANY of the values matches
                    if (checkMatch(val, value2Check))   //recursive with each element
                        return true;
                }
                return false;
            }
            else
            {
                //value to check is IEnumerable
                if (value2Check is IEnumerable && !(value2Check is string))
                {
                    //Check every possible value and see if ANY of the matches
                    foreach (object val in value2Check as IEnumerable)
                    {
                        if (checkMatch(propertyValue, val))
                            return true;
                    }
                    return false;
                }
                else
                {
                    //Any other value, check for equality
                    if (value2Check is string)
                    {
                        //In the case of strings, match without leading spaces and case insensitive
                        return propertyValue.ToString().Trim().Equals(value2Check.ToString().Trim(), StringComparison.CurrentCultureIgnoreCase);
                    }
                    else
                        return propertyValue.Equals(value2Check);
                }
            }
        }

        //Returns the correct C# name from a Ruby name
        private static string GetCSharpName(string name)
        {
            string res = name;
            //Only change it if we're using the Ruby naming convention
            if (Template.NamingConvention is DotLiquid.NamingConventions.RubyNamingConvention)
            {
                Regex reRubyNameSeparators = new Regex(@"(_([a-z]))",
                    RegexOptions.IgnoreCase |
                    RegexOptions.Multiline |
                    RegexOptions.CultureInvariant);

                //Find all the "_" and substitute the next letter for an Uppercase one
                res = reRubyNameSeparators.Replace(name, m => m.Groups[2].Value.ToUpper());
                //Change the first letter too if needed
                res = res.First().ToString().ToUpper() + res.Substring(1);
            }

            return res;
        }
        #endregion
    }
}