using System;

namespace IISHelpers
{
    internal static class TypesHelper
    {

        /// <summary>
        /// Tries to convert any object to the specified type
        /// </summary>
        internal static T DoConvert<T>(object v)
        {
            try
            {
                return (T)Convert.ChangeType(v, typeof(T));
            }
            catch
            {
                return (T)Activator.CreateInstance(typeof(T));
            }
        }

        /// <summary>
        /// Determines if a string value (normally got from params or Front-Matter)
        /// representes a "true" value or not.
        /// Valid "true" values are: "1", "true" and "yes"
        /// </summary>
        /// <param name="val">The value to be checked</param>
        /// <returns>true if is a truthy value according to the criteria</returns>
        internal static bool IsTruthy(string val)
        {
            val = val.ToLower();
            return (val == "1" || val == "true" || val == "yes");
        }

        /// <summary>
        /// Determines if a string value (normally got from params or Front-Matter)
        /// representes a "false" value or not.
        /// Valid "false" values are: "0", "false" and "no"
        /// </summary>
        /// <param name="val">The value to be checked</param>
        /// <returns>true if is a truthy value according to the criteria</returns>
        internal static bool IsFalsy(string val)
        {
            return (val == "0" || val == "false" || val == "no");
        }
    }
}
