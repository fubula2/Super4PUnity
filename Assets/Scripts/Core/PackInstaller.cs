using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

public enum ConflictChoice { Overwrite, Skip }

public sealed class InstallResult
{
    public bool Ok;
    public string Error;
    public IReadOnlyList<string> Installed = new List<string>();
    public IReadOnlyList<string> Skipped = new List<string>();
}

//Extracts a pack into a temp folder and moves its CHAR_* mod folders into the mods folder
public static class PackInstaller
{
    //onSelect receives the CHAR_* folder names found in the pack and returns the ones to import (null/empty cancels)
    public static InstallResult Install(string archivePath, string modsDir, string tempRoot, Func<IReadOnlyList<string>, IReadOnlyList<string>> onSelect,
        Func<IReadOnlyList<string>, ConflictChoice> onConflict, IProgress<float> progress, CancellationToken ct)
    {
        var temp = Path.Combine(tempRoot, Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(temp);
            var extracted = ArchiveExtractor.Extract(archivePath, temp, progress, ct);
            if(!extracted.Ok) { return Fail(extracted.Error); }

            var dirs = PackLocator.FindCharDirs(temp);
            if(dirs.Count == 0) { return Fail("This pack contains no CHAR_ mod folders, so nothing was installed."); }

            var offered = dirs.Select(Path.GetFileName).Distinct().ToList();
            var chosen = onSelect(offered);
            if(chosen == null || chosen.Count == 0) { return Fail("Cancelled."); }

            var chosenSet = new HashSet<string>(chosen);
            dirs = dirs.Where(d => chosenSet.Contains(Path.GetFileName(d))).ToList();
            if(dirs.Count == 0) { return Fail("Cancelled."); }

            Directory.CreateDirectory(modsDir);
            var names = dirs.Select(Path.GetFileName).Distinct().ToList();
            var existing = names.Where(n => Directory.Exists(Path.Combine(modsDir, n))).ToList();
            var choice = existing.Count > 0 ? onConflict(existing) : ConflictChoice.Overwrite;

            var installed = new List<string>();
            var skipped = new List<string>();
            foreach(var dir in dirs)
            {
                ct.ThrowIfCancellationRequested();
                var name = Path.GetFileName(dir);
                var dest = Path.Combine(modsDir, name);
                var isDuplicate = installed.Contains(name) || skipped.Contains(name);
                var exists = Directory.Exists(dest);

                if(isDuplicate || (exists && choice == ConflictChoice.Skip)) { skipped.Add(name); continue; }
                if(exists) { Directory.Delete(dest, true); }
                MoveDirectory(dir, dest);
                installed.Add(name);
            }
            return new InstallResult { Ok = true, Installed = installed, Skipped = skipped };
        }
        catch(OperationCanceledException) { return Fail("Cancelled."); }
        catch(Exception e) when(e is IOException || e is UnauthorizedAccessException) { return Fail("Could not install the pack: " + e.Message); }
        finally
        {
            try { if(Directory.Exists(temp)) { Directory.Delete(temp, true); } }
            catch(IOException) { }
            catch(UnauthorizedAccessException) { }
        }
    }

    private static InstallResult Fail(string message)
    {
        return new InstallResult { Ok = false, Error = message };
    }

    private static void MoveDirectory(string source, string dest)
    {
        try { Directory.Move(source, dest); }
        catch(IOException)
        {
            //Different volume: copy then delete
            CopyDirectory(source, dest);
            Directory.Delete(source, true);
        }
    }

    private static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach(var file in Directory.GetFiles(source)) { File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), true); }
        foreach(var dir in Directory.GetDirectories(source)) { CopyDirectory(dir, Path.Combine(dest, Path.GetFileName(dir))); }
    }
}
