using System;
using System.Net.Http;

//One shared HttpClient. Timeouts are applied per request so long file downloads are not cut off.
public static class NetClient
{
    public static readonly TimeSpan PageTimeout = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan SheetTimeout = TimeSpan.FromSeconds(120);

    public static readonly HttpClient Http = Create();

    private static HttpClient Create()
    {
        var client = new HttpClient { Timeout = System.Threading.Timeout.InfiniteTimeSpan };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; Super4PUnity)");
        return client;
    }
}
