using System;

namespace MIISHandler.Filters
{
    //A marked Interface to detect easily all the custom filters when loading
    //I know this is a little bit of a code smell, but in dotLiquid, filters don't implement any interface (custom tags do)
    //so, for loading, I think this is the best way to locate them fast and reliably
    public interface IFilterFactory
    {
        //Returns the type to be used as a filter for DotLiquid
        Type GetFilterType();
    }
}
