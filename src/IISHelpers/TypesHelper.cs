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
    }
}
