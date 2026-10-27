using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

//Loads the mod library sheet: fresh cache if recent, otherwise the live export, falling back to a stale cache
public sealed class LibraryService
{
    public const string SheetUrl = "https://docs.google.com/spreadsheets/d/1kAkBLDDOEF-v0_LPNFYi4IwR0NpWBF575WicjG6tEIw/export?format=xlsx";
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(24);

    private readonly string cachePath;

    //cacheDir is read from Application.persistentDataPath by the caller (main thread only)
    public LibraryService(string cacheDir)
    {
        cachePath = Path.Combine(cacheDir, "library.xlsx");
    }

    public async Task<LibraryCatalog> LoadAsync(bool forceRefresh, CancellationToken ct)
    {
        var hasCache = File.Exists(cachePath);

        if(!forceRefresh && hasCache && DateTime.UtcNow - File.GetLastWriteTimeUtc(cachePath) < CacheLifetime)
        {
            var cached = await Task.Run(() => ParseCache(), ct).ConfigureAwait(false);
            if(cached.Error == null) { return cached; }
        }

        try
        {
            await FetchAsync(ct).ConfigureAwait(false);
        }
        catch(Exception e) when(e is HttpRequestException || e is TaskCanceledException || e is IOException)
        {
            if(ct.IsCancellationRequested) { throw new OperationCanceledException(ct); }

            if(hasCache || File.Exists(cachePath))
            {
                var fallback = await Task.Run(() => ParseCache(), ct).ConfigureAwait(false);
                if(fallback.Error == null)
                {
                    fallback.Warning = "Could not reach the library sheet, showing the saved list. (" + e.Message + ")";
                    return fallback;
                }
            }
            return new LibraryCatalog { Error = "Could not load the mod library: " + e.Message };
        }

        return await Task.Run(() => ParseCache(), ct).ConfigureAwait(false);
    }

    private LibraryCatalog ParseCache()
    {
        using(var stream = File.OpenRead(cachePath)) { return LibrarySheetParser.Parse(stream, Path.Combine(Path.GetDirectoryName(cachePath), "icons")); }
    }

    private async Task FetchAsync(CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
        var temp = cachePath + ".part";

        using(var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct))
        {
            timeout.CancelAfter(NetClient.SheetTimeout);
            try
            {
                using(var response = await NetClient.Http.GetAsync(SheetUrl, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    using(var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using(var output = File.Create(temp)) { await input.CopyToAsync(output, 1 << 20, timeout.Token).ConfigureAwait(false); }
                }
                if(File.Exists(cachePath)) { File.Delete(cachePath); }
                File.Move(temp, cachePath);
            }
            finally
            {
                if(File.Exists(temp)) { File.Delete(temp); }
            }
        }
    }
}
