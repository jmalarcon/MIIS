using DotLiquid;
using System;
using System.Web;

namespace MIISHandler.Filters
{

    //Needed by MIIS to add the correct reference to DotLiquid
    public class UrlFilterFactory : IFilterFactory
    {
        Type IFilterFactory.GetFilterType()
        {
            return typeof(UrlFilters);
        }
    }

    public static class UrlFilters
    {
        /// <summary>
        /// This filter takes a url relative to the current file and returns a relative URL from the base folder of the site
        /// For example: "images/myimg.png" --> "/documents/support/images/myimg.png"
        /// It supports full absolute URLs (with the protocol and the domain: unchanged), root based URLs with ~/ and relative URLs
        /// </summary>
        /// <param name="input">A string with the path to transform</param>
        public static string RelativeUrl(string input)
        {
            //If it's already an absolute URL, just return it
            if (input.ToLower().StartsWith("http://") || input.ToLower().StartsWith("https://"))
                return input;

            //Now check if it's a relative URl or not (it starts with ~/ or not)
            if (input.StartsWith("~/"))
                return VirtualPathUtility.ToAbsolute(input);

            //Finally, if it's a relative URL, it'll be relative to the current request path, so transform it acordingly
            //Get current file's base folder
            string currentFolderBasepath = VirtualPathUtility.GetDirectory(VirtualPathUtility.ToAbsolute(HttpContext.Current.Request.Url.LocalPath));
            //combine the base with the specified path
            return VirtualPathUtility.Combine(currentFolderBasepath, input);
        }

        /// <summary>
        /// This filter takes a url relative to the current file and returns an absolute  URL with protocol and domain included
        /// For example: "images/myimg.png" --> "https://www.mysite.com/documents/support/images/myimg.png"
        /// It supports full absolute URLs (with the protocol and the domain: unchanged), root based URLs with ~/ and relative URLs
        /// </summary>
        /// <param name="input">A string with the path to transform</param>
        public static string AbsoluteUrl(string input)
        {
            //If it's already an absolute URL, just return it
            if (input.ToLower().StartsWith("http://") || input.ToLower().StartsWith("https://"))
                return input;

            //Get the relative URL
            string relUrl = RelativeUrl(input);
            //And transform it to an absolute URL
            HttpContext _ctx = HttpContext.Current;
            return $"{_ctx.Request.Url.Scheme}{System.Uri.SchemeDelimiter}{_ctx.Request.Url.Authority}{relUrl}";
        }
    }
}