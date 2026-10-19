using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public enum UpdateStatus { UpToDate, Available, NoReleases, Failed }

public sealed class UpdateResult
{
    public UpdateStatus Status;
    public ReleaseInfo Release;
    public string Error;
    public bool Manual;
}

//Asks GitHub for the latest published release and reports whether it is newer. Notify-only: nothing is downloaded or installed.
public static class UpdateChecker
{
    //Where releases are published. This is the only place the repository path appears.
    public const string ReleasesApiUrl = "https://api.github.com/repos/fubula2/Super4PUnity/releases/latest";

    private static readonly ConcurrentQueue<UpdateResult> Results = new ConcurrentQueue<UpdateResult>();
    private static int running;

    public static bool IsRunning => Volatile.Read(ref running) != 0;

    private static string Url
    {
        get
        {
            #if DEVELOPMENT_BUILD || UNITY_EDITOR
                var overrideUrl = Environment.GetEnvironmentVariable("S4P_UPDATE_URL");
                if(!string.IsNullOrEmpty(overrideUrl)) { return overrideUrl; }
            #endif
            return ReleasesApiUrl;
        }
    }

    public static void Check(string currentVersion, bool manual)
    {
        if(Interlocked.CompareExchange(ref running, 1, 0) != 0) { return; }
        var url = Url;
        Task.Run(() => Run(url, currentVersion, manual));
    }

    public static bool TryTakeResult(out UpdateResult result) { return Results.TryDequeue(out result); }

    private static async Task Run(string url, string currentVersion, bool manual)
    {
        var result = new UpdateResult { Manual = manual };
        try
        {
            using(var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
            using(var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.Accept.ParseAdd("application/vnd.github+json");
                using(var response = await NetClient.Http.SendAsync(request, cts.Token).ConfigureAwait(false))
                {
                    if(response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        result.Status = UpdateStatus.NoReleases;
                    }
                    else
                    {
                        response.EnsureSuccessStatusCode();
                        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var release = ReleaseParser.Parse(json);
                        if(release.Error != null) { result.Status = UpdateStatus.NoReleases; }
                        else
                        {
                            result.Release = release;
                            result.Status = AppVersion.IsNewer(release.Version, currentVersion) ? UpdateStatus.Available : UpdateStatus.UpToDate;
                        }
                    }
                }
            }
        }
        catch(Exception e) when(e is HttpRequestException || e is TaskCanceledException || e is OperationCanceledException)
        {
            result.Status = UpdateStatus.Failed;
            result.Error = e is HttpRequestException ? e.Message : "the request timed out";
        }
        finally
        {
            Results.Enqueue(result);
            Volatile.Write(ref running, 0);
        }
    }
}
