using System;
using System.Linq;
using System.Collections;

namespace MIISHandler.Filters
{

    //Needed by MIIS to add the correct reference to DotLiquid
    public class ConcatFilterFactory : IFilterFactory
    {
        Type IFilterFactory.GetFilterType()
        {
            return typeof(ConcatFilter);
        }
    }

    public static class ConcatFilter
    {
        /// <summary>
        /// This filter takes two IEnumerables (array, IList...) and concats them to form a new one
        /// with all the elements together in a single collection
        /// </summary>
        /// <param name="input">Only an IEnumerable makes sense in this kind of filtering</param>
        /// <param name="input2">Another IEnumerable to concatenate</param>
        /// <param name="uniq">A boolean to define if the resulting list should include only unique items</returns>
        public static IEnumerable Concat(IEnumerable input, IEnumerable input2, bool uniq = false)
        {
            var res = input.Cast<object>().Concat(input2.Cast<object>());
            if (uniq)
                res = res.Distinct();
            
            return res;
        }
    }
}