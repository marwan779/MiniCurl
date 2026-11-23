using MiniCurl.Hsts;
using MiniCurl.Parsing;
using System;

class Program
{
    static void Main(string[] args)
    {
        var parser = new URLParser();

        HstsCahceProvider hstsCahceProvider = new HstsCahceProvider();


        //var item = new HstsCacheItem(
        //    "github.com",
        //    TimeSpan.FromSeconds(31536000),
        //    includeSubDomains: true
        //);

        //var res = hstsCahceProvider.Set( item );

        hstsCahceProvider.Delete("github.com");

    }
}
