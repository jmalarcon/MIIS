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

        /// <summary>
        /// Quick and dirty parser for dates in Universal Sortable DateTime Format, such as the ones used normally in YAML and Jekyll.
        /// It can parse dates only in this three specific forms:
        /// 2019-12-01 --> Just the date
        /// 2019-12-01 13:45 --> Date and time in short form without seconds
        /// 2019-12-01 13:45:16 --> Data and time in short form with seconds
        /// Any other format will fail and the default time will be returned
        /// </summary>
        /// <param name="sd">A string with the date to parse</param>
        /// <param name="defValue">The default value to return in case the string is not parseable</param>
        /// <returns>The date in the string or the default value if its not parseable</returns>
        internal static DateTime ParseUniversalSortableDateTimeString(string sd, DateTime defValue)
        {
            DateTime res;
            sd = sd.Trim();
            bool parsed = DateTime.TryParseExact(sd, "yyyy'-'MM-dd", null, System.Globalization.DateTimeStyles.None, out res);
            if (!parsed)
            {
                //Try with full-time
                parsed = DateTime.TryParseExact(sd, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out res);
                if (!parsed)
                {
                    //Try with time (not seconds)
                    parsed = DateTime.TryParseExact(sd, "yyyy-MM-dd HH:mm", null, System.Globalization.DateTimeStyles.None, out res);
                }
            }
            return parsed ? res : defValue;
        }
    }
}
