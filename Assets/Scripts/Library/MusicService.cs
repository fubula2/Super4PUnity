using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

//Loads music folder listings from the provider the sheet's link points at; listings are cached for the session.
public sealed class MusicService
{
    public static readonly IReadOnlyList<IMusicSource> Sources = new IMusicSource[] { new DriveLinks() };

    private readonly Dictionary<string, IReadOnlyList<MusicEntry>> cache = new Dictionary<string, IReadOnlyList<MusicEntry>>();

    public IMusicSource Source { get; }
    public string RootId { get; }

    //Source is null when the link is missing or its provider is not supported
    public MusicService(string folderUrl)
    {
        Source = MusicSourceSelector.ForUrl(folderUrl, Sources);
        RootId = Source?.RootFolderId(folderUrl);
    }

    public bool IsUsable => Source != null && RootId != null;

    //Throws HttpRequestException when the listing cannot be fetched
    public async Task<IReadOnlyList<MusicEntry>> ListAsync(string folderId, CancellationToken ct)
    {
        lock(cache) { if(cache.TryGetValue(folderId, out var cached)) { return cached; } }
        using(var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct))
        {
            timeout.CancelAfter(NetClient.PageTimeout);
            try
            {
                using(var response = await NetClient.Http.GetAsync(Source.ListingUrl(folderId), timeout.Token).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var entries = Source.ParseListing(body);
                    lock(cache) { cache[folderId] = entries; }
                    return entries;
                }
            }
            catch(TaskCanceledException) when(!ct.IsCancellationRequested) { throw new HttpRequestException("The request timed out."); }
        }
    }
}
