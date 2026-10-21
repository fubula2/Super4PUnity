using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

//What the player is asked to confirm after the picked archives/folders were staged
public sealed class ImportPreview
{
    public IReadOnlyList<string> Names = new List<string>();       //CHAR_* folder names found
    public IReadOnlyList<string> NotMods = new List<string>();     //sources without any CHAR_ folder
    public IReadOnlyList<string> Unreadable = new List<string>();  //archives that could not be extracted
    public IReadOnlyList<string> Duplicates = new List<string>();  //names found again in a later source (ignored)
}

//Stages several archives/folders picked on the device and installs the CHAR_* mod folders found in them
public static class ImportInstaller
{
    public static InstallResult Install(IReadOnlyList<string> sources, string modsDir, string tempRoot, Func<ImportPreview, IReadOnlyList<string>> onSelect,
        Func<IReadOnlyList<string>, ConflictChoice> onConflict, IProgress<float> progress, CancellationToken ct)
    {
        var temp = Path.Combine(tempRoot, Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(temp);
            var notMods = new List<string>();
            var unreadable = new List<string>();
            var duplicates = new List<string>();
            var dirs = new List<string>();
            var taken = new HashSet<string>(StringComparer.Ordinal);

            for(var i = 0; i < sources.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var source = sources[i];
                var label = Path.GetFileName(source.TrimEnd('/', '\\'));
                var stage = Path.Combine(temp, i.ToString());
                var index = i;
                var inner = new InlineProgress<float>(f => progress?.Report((index + f) / sources.Count));

                if(ImportSources.Classify(source) == ImportSourceKind.Folder)
                {
                    try { PackInstaller.CopyDirectory(source, Path.Combine(stage, label)); }
                    catch(Exception e) when(e is IOException || e is UnauthorizedAccessException) { unreadable.Add(label); continue; }
                }
                else
                {
                    var extracted = ArchiveExtractor.Extract(source, stage, inner, ct);
                    if(!extracted.Ok)
                    {
                        if(extracted.Error == "Cancelled.") { throw new OperationCanceledException(); }
                        unreadable.Add(label);
                        continue;
                    }
                }

                var found = PackLocator.FindCharDirs(stage);
                if(found.Count == 0) { notMods.Add(label); }
                foreach(var dir in found)
                {
                    var name = Path.GetFileName(dir);
                    if(taken.Add(name)) { dirs.Add(dir); }
                    else if(!duplicates.Contains(name)) { duplicates.Add(name); }
                }
                progress?.Report((float)(i + 1) / sources.Count);
            }

            if(dirs.Count == 0) { return Fail(NothingFound(notMods, unreadable)); }

            var preview = new ImportPreview { Names = dirs.Select(Path.GetFileName).ToList(), NotMods = notMods, Unreadable = unreadable, Duplicates = duplicates };
            var chosen = onSelect(preview);
            if(chosen == null || chosen.Count == 0) { return Fail("Cancelled."); }

            var chosenSet = new HashSet<string>(chosen);
            dirs = dirs.Where(d => chosenSet.Contains(Path.GetFileName(d))).ToList();
            if(dirs.Count == 0) { return Fail("Cancelled."); }

            return PackInstaller.InstallDirs(dirs, modsDir, onConflict, ct);
        }
        catch(OperationCanceledException) { return Fail("Cancelled."); }
        catch(Exception e) when(e is IOException || e is UnauthorizedAccessException) { return Fail("Could not import: " + e.Message); }
        finally
        {
            try { if(Directory.Exists(temp)) { Directory.Delete(temp, true); } }
            catch(IOException) { }
            catch(UnauthorizedAccessException) { }
        }
    }

    private static string NothingFound(List<string> notMods, List<string> unreadable)
    {
        var text = "No CHAR_ mod folders were found, so nothing was imported.";
        if(unreadable.Count > 0) { text += " Could not read: " + string.Join(", ", unreadable) + "."; }
        if(notMods.Count > 0) { text += " Not mods: " + string.Join(", ", notMods) + "."; }
        return text;
    }

    private static InstallResult Fail(string message)
    {
        return new InstallResult { Ok = false, Error = message };
    }
}
