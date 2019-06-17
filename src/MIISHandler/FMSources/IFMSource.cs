using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotLiquid;

namespace MIISHandler.FMSources
{
    //This must be implemented by Front-Matter Sources that allow to add content fields 
    //on the spot from external resources, allowing to inject objects and data in any .md or .mdh doc
    public interface IFMSource
    {
        //This property needs to return a non-empty string with the name of the Front_Matter source, so that you can use it from the FM
        //It doesn't allow spaces and it 's not case sensitive
        string SourceName { get; }

        //This method gets the param value. It can return any type, but to be able to work correctly 
        //and to be used in contents, it must be a basic type, inherit from Drop, implement ILiquidizable, etc...
        //See: https://github.com/dotliquid/dotliquid/wiki/DotLiquid-for-Developers#rules-for-template-rendering-parameters
        //If it doesn't comply with this, you'll get an error when trying to use it
        //It takes a variable number of string parameters extracted from the declarion on the file
        object GetValue(MIISFile currentFile, params string[] parameters);
    }
}
