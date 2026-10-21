using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

//Copies music files picked on the device flat into the music folder; the originals are only read
public static class MusicImporter
{
    public static InstallResult Import(IReadOnlyList<string> files, string musicDir, Func<IReadOnlyList<string>, ConflictChoice> onConflict, IProgress<float> progress, CancellationToken ct)
    {
        try
        {
            Directory.CreateDirectory(musicDir);
            var existing = new List<string>();
            foreach(var f in files) { if(File.Exists(Path.Combine(musicDir, Path.GetFileName(f)))) { existing.Add(Path.GetFileName(f)); } }
            var choice = existing.Count > 0 ? onConflict(existing) : ConflictChoice.Overwrite;

            var installed = new List<string>();
            var skipped = new List<string>();
            for(var i = 0; i < files.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var name = Path.GetFileName(files[i]);
                var dest = Path.Combine(musicDir, name);
                if(installed.Contains(name) || (File.Exists(dest) && choice == ConflictChoice.Skip)) { skipped.Add(name); continue; }
                var part = dest + ".part";
                File.Copy(files[i], part, true);
                if(File.Exists(dest)) { File.Delete(dest); }
                File.Move(part, dest);
                installed.Add(name);
                progress?.Report((float)(i + 1) / files.Count);
            }
            return new InstallResult { Ok = true, Installed = installed, Skipped = skipped };
        }
        catch(OperationCanceledException) { return new InstallResult { Error = "Cancelled." }; }
        catch(Exception e) when(e is IOException || e is UnauthorizedAccessException) { return new InstallResult { Error = "Could not copy the music: " + e.Message }; }
    }
}
