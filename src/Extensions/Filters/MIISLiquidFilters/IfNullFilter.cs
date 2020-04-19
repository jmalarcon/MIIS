using System;
using System.Linq;
using System.Collections;

namespace MIISHandler.Filters
{

    //Needed by MIIS to add the correct reference to DotLiquid
    public class IfNullFilterFactory : IFilterFactory
    {
        Type IFilterFactory.GetFilterType()
        {
            return typeof(IfNullFilter);
        }
    }

    public static class IfNullFilter
    {
        /// <summary>
        /// This filter returns the value if it's not null, or the parameter if the value to filter it's null
        /// Very useful to use default values. It can be used several times in the same tag, and saves lots of if-else-end Liquid tags
        /// </summary>
        /// <param name="input">Any parameter</param>
        public static object IfNull(object input, object defValue)
        {
            return input ?? defValue;
        }
    }
}