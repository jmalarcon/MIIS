using System;
using System.Globalization;

namespace MIISHandler.Filters
{
    //Needed by MIIS to add the correct reference to DotLiquid
    public class TitleCaseFilterFactory : IFilterFactory
    {
        Type IFilterFactory.GetFilterType()
        {
            return typeof(TitleCaseFilter);
        }
    }

    //This is the current filter implementation
    public static class TitleCaseFilter
    {
        //Warning: in DotLiquid, filter names are converted to snake_case. So, to use this filter, you should write "{{ field | title_case }}"
        public static string TitleCase(string input)
        {
            TextInfo ti = CultureInfo.CurrentCulture.TextInfo;
            return ti.ToTitleCase(input.ToLower());
        }
    }
}
