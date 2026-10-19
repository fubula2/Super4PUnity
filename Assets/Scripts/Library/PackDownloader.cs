using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

//MediaFire access: folder listings, file page -> direct URL, and streamed downloads.
//Every await uses ConfigureAwait(false) so continuations never wait for Unity's main thread.
public sealed class PackDownloader
{
    private const int MaxFolderChunks = 50;
    private const int BufferSize = 1 << 20;

    public async Task<FolderListing> ListFolderAsync(string folderUrl, CancellationToken ct)
    {
        var key = MediaFirePage.FolderKey(folderUrl);
        if(key == null || !MediaFirePage.IsMediaFireHttps(folderUrl)) { return new FolderListing { Error = "This is not a MediaFire folder link." }; }

        var files = new List<FolderFile>();
        for(var chunk = 1; chunk <= MaxFolderChunks; chunk++)
        {
            string json;
            try { json = await GetStringAsync(MediaFirePage.FolderApiUrl(key, chunk), ct).ConfigureAwait(false); }
            catch(HttpRequestException e) { return new FolderListing { Error = "Could not reach MediaFire: " + e.Message }; }

            var page = MediaFireFolderJson.Parse(json);
            if(page.Error != null) { return new FolderListing { Error = page.Error }; }
            files.AddRange(page.Files);
            if(!page.MoreChunks) { break; }
        }
        return new FolderListing { Files = files };
    }

    public async Task<string> ResolveDirectUrlAsync(string pageUrl, CancellationToken ct)
    {
        if(!MediaFirePage.IsMediaFireHttps(pageUrl)) { throw new InvalidOperationException("Only MediaFire links can be downloaded."); }

        //Only the headers are read first: a file link may redirect straight to a download of tens of megabytes, which must not be
        //read as text. A redirect ends up on a download address; a web page is read for its download button.
        using(var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct))
        {
            timeout.CancelAfter(NetClient.PageTimeout);
            try
            {
                using(var response = await NetClient.Http.GetAsync(pageUrl, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    var final = response.RequestMessage?.RequestUri?.ToString();
                    var mediaType = response.Content.Headers.ContentType?.MediaType;
                    if(!MediaFirePage.IsHtmlPage(mediaType))
                    {
                        if(MediaFirePage.IsMediaFireHttps(final)) { return final; }
                        throw new InvalidOperationException("MediaFire sent the file from an unexpected address, so it was not downloaded.");
                    }

                    var html = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if(!MediaFirePage.TryGetDirectUrl(html, out var url))
                    {
                        if(MediaFirePage.IsRepairPage(html)) { throw new InvalidOperationException("MediaFire is still preparing a download link for this file. Try again in a minute or two."); }
                        throw new InvalidOperationException("MediaFire did not provide a download link for this file. It may have been removed or needs a browser.");
                    }
                    return url;
                }
            }
            catch(TaskCanceledException) when(!ct.IsCancellationRequested)
            {
                throw new HttpRequestException("The request timed out.");
            }
        }
    }

    //Streams to <destDir>/<name>; returns the final path. The .part file is removed on any failure.
    //onBytes(done, total) runs on the download thread; total is 0 when unknown.
    public async Task<string> DownloadAsync(string directUrl, string destDir, long expectedBytes, Action<long, long> onBytes, CancellationToken ct)
    {
        if(!MediaFirePage.IsMediaFireHttps(directUrl)) { throw new InvalidOperationException("Refusing to download from a non-MediaFire address."); }

        var fileName = SafeFileName(directUrl);
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if(ext != ".zip" && ext != ".rar") { throw new InvalidOperationException("This pack is a '" + ext + "' file, which the app cannot extract."); }

        Directory.CreateDirectory(destDir);
        var final = Path.Combine(destDir, fileName);
        var part = Path.Combine(destDir, Guid.NewGuid().ToString("N") + ".part");

        try
        {
            using(var response = await NetClient.Http.GetAsync(directUrl, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                var total = response.Content.Headers.ContentLength ?? expectedBytes;

                var free = DiskInfo.FreeBytes(destDir);
                if(total > 0 && free.HasValue && !DiskSpace.HasRoom(free.Value, total))
                {
                    throw new InvalidOperationException("Not enough free disk space: this pack needs about " + TransferFormat.Size((long)(total * DiskSpace.Factor)) + ", " + TransferFormat.Size(free.Value) + " free.");
                }

                using(var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using(var output = new FileStream(part, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous))
                {
                    var buffer = new byte[BufferSize];
                    long done = 0;
                    int read;
                    while((read = await input.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
                    {
                        await output.WriteAsync(buffer, 0, read, ct).ConfigureAwait(false);
                        done += read;
                        onBytes?.Invoke(done, total);
                    }
                }
            }
            if(File.Exists(final)) { File.Delete(final); }
            File.Move(part, final);
            return final;
        }
        finally
        {
            if(File.Exists(part)) { File.Delete(part); }
        }
    }

    public static string FormatSize(long bytes) { return TransferFormat.Size(bytes); }

    private static string SafeFileName(string url)
    {
        var name = Uri.UnescapeDataString(Path.GetFileName(new Uri(url).AbsolutePath));
        foreach(var c in Path.GetInvalidFileNameChars()) { name = name.Replace(c, '_'); }
        return string.IsNullOrWhiteSpace(name) ? "pack.zip" : name;
    }

    private static async Task<string> GetStringAsync(string url, CancellationToken ct)
    {
        using(var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct))
        {
            timeout.CancelAfter(NetClient.PageTimeout);
            try
            {
                using(var response = await NetClient.Http.GetAsync(url, timeout.Token).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
            catch(TaskCanceledException) when(!ct.IsCancellationRequested)
            {
                throw new HttpRequestException("The request timed out.");
            }
        }
    }
}
