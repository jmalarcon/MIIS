using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MIISHandler.FMSources
{
    public interface IQueryStringDependent
    {
        /// <summary>
        /// This method allows to register any QueryString fields that are needed to cache
        /// the rendered results for a front matter source. 
        /// If a field is not registered here or in the WellKnownFields global
        /// param for the app, it will not be considered for caching
        /// </summary>
        /// <returns>An array of strings with the names of fields in 
        /// the query string this front-matter source depends on</returns>
        string[] GetCachingQueryStringFields();
    }
}