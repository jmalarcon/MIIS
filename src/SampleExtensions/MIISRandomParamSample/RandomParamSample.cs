using System;

namespace MIISHandler.FMSources
{
    //This is a simple sample Front-Matter parameter provider.
    //It returns a random integer value between two specified values (the range)
    //If no range is specified, it returns a number between 0 and 100
    //If only one number is specified, then it'll return a number between 0 and that number
    //If numbers can't be parsed to integers, an exception will be thrown
    public class RandomParamSample : IFMSource
    {

        //The tag that will activate this custom FM param
        string IFMSource.SourceName => "random_int";

        object IFMSource.GetValue(MIISFile currentFile, params string[] srcParams)
        {
            int min = 0, max = 100;

            //Parse paramenters
            if (srcParams.Length == 1)
                max = int.Parse(srcParams[0]);

            if (srcParams.Length == 2)
            {
                min = int.Parse(srcParams[0]);
                max = int.Parse(srcParams[1]);
            }

            if (min > max)  //Swap them
            {
                var t = max;
                max = min;
                min = t;
            }

            Random rnd = new Random();

            //Force the file to be cached for 10 seconds
            //currentFile.SetMaxCacheValidity(10);
            //Force the file to not do any caching despite of the UseMDCaching parameter
            //currentFile.DisableCache();

            return rnd.Next(min, max);
        }
    }
}
