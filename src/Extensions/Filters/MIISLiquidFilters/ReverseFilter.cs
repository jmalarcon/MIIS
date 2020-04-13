using System;
using System.Linq;
using System.Collections;

namespace MIISHandler.Filters
{

    //Needed by MIIS to add the correct reference to DotLiquid
    public class ReverseFilterFactory : IFilterFactory
    {
        Type IFilterFactory.GetFilterType()
        {
            return typeof(ReverseFilter);
        }
    }

    public static class ReverseFilter
    {
        /// <summary>
        /// This filter takes an IEnumerables (array, IList...) and returns the same one but in reverse order
        /// </summary>
        /// <param name="input">Only an IEnumerable makes sense in this kind of filtering</param>
        public static IEnumerable Reverse(IEnumerable input)
        {
            var res = input.Cast<object>().Reverse();
            return res;
        }
    }
}