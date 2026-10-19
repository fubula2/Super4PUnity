using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using SharpCompress.Readers;

public sealed class ExtractResult
{
    public bool Ok;
    public string Error;
    public int Files;
    public int Skipped;
}

//Extracts .zip and .rar archives; every entry path goes through SafeExtract and unsafe ones are skipped
public static class ArchiveExtractor
{
    public static ExtractResult Extract(string archivePath, string targetDir, IProgress<float> progress, CancellationToken ct)
    {
        var result = new ExtractResult();
        try
        {
            if(!File.Exists(archivePath)) { return Fail(result, "The downloaded file is missing."); }

            var ext = Path.GetExtension(archivePath).ToLowerInvariant();
            Directory.CreateDirectory(targetDir);
            if(ext == ".zip") { ExtractZip(archivePath, targetDir, progress, ct, result); }
            else if(ext == ".rar") { ExtractRar(archivePath, targetDir, progress, ct, result); }
            else { return Fail(result, "Unsupported archive type '" + ext + "'."); }

            result.Ok = true;
            return result;
        }
        catch(OperationCanceledException) { return Fail(result, "Cancelled."); }
        catch(Exception e) when(e is InvalidDataException || e is IOException || e is UnauthorizedAccessException || e is NotSupportedException || e is SharpCompress.Common.SharpCompressException || e is ArgumentException)
        {
            return Fail(result, "Could not extract the archive: " + e.Message);
        }
    }

    private static ExtractResult Fail(ExtractResult result, string message)
    {
        result.Ok = false;
        result.Error = message;
        return result;
    }

    private static void ExtractZip(string path, string target, IProgress<float> progress, CancellationToken ct, ExtractResult result)
    {
        using(var zip = ZipFile.OpenRead(path))
        {
            var done = 0;
            var total = Math.Max(1, zip.Entries.Count);
            foreach(var entry in zip.Entries)
            {
                ct.ThrowIfCancellationRequested();
                if(!SafeExtract.TryResolve(target, entry.FullName, out var full)) { result.Skipped++; }
                else if(entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\")) { Directory.CreateDirectory(full); }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(full));
                    using(var input = entry.Open())
                    using(var output = File.Create(full)) { CopyWithCancel(input, output, ct); }
                    result.Files++;
                }
                progress?.Report(++done / (float)total);
            }
        }
    }

    private static void ExtractRar(string path, string target, IProgress<float> progress, CancellationToken ct, ExtractResult result)
    {
        using(var stream = File.OpenRead(path))
        using(var reader = ReaderFactory.Open(stream))
        {
            while(reader.MoveToNextEntry())
            {
                ct.ThrowIfCancellationRequested();
                var entry = reader.Entry;
                if(!SafeExtract.TryResolve(target, entry.Key, out var full)) { result.Skipped++; }
                else if(entry.IsDirectory) { Directory.CreateDirectory(full); }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(full));
                    using(var output = File.Create(full))
                    using(var input = reader.OpenEntryStream()) { CopyWithCancel(input, output, ct); }
                    result.Files++;
                }
                progress?.Report(stream.Length == 0 ? 1f : (float)stream.Position / stream.Length);
            }
        }
    }

    private static void CopyWithCancel(Stream input, Stream output, CancellationToken ct)
    {
        var buffer = new byte[81920];
        int read;
        while((read = input.Read(buffer, 0, buffer.Length)) > 0)
        {
            ct.ThrowIfCancellationRequested();
            output.Write(buffer, 0, read);
        }
    }
}
