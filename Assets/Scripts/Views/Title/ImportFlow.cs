using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

//"Import from device": chooser -> picker -> confirm -> background import (DownloadManager shows the second confirmation)
public class ImportFlow
{
    private readonly Dialog dialog;
    private readonly GameObject host;
    private IImportPicker picker;
    private readonly List<string> picked = new List<string>();
    private int ignored;
    private bool music;

    public ImportFlow(Dialog dialog, GameObject host)
    {
        this.dialog = dialog;
        this.host = host;
    }

    //The app was opened with these files (Android "Open with" or Share): go straight to the confirmation
    public void StartWithPaths(IReadOnlyList<string> paths)
    {
        picker = picker ?? ImportPickers.Create(host);
        if(picker == null) { return; }
        music = false;
        picked.Clear();
        ignored = 0;
        OnPicked(paths);
    }

    //Asks whether characters or music are being imported, then opens the picker
    public void Start()
    {
        if(dialog.IsOpen) { return; }
        picker = picker ?? ImportPickers.Create(host);
        if(picker == null) { dialog.Show("Import files", "Importing is not available on this device.", "OK", null); return; }
        if(picker.UnavailableReason != null) { dialog.Show("Import files", picker.UnavailableReason, "OK", null); return; }

        dialog.ShowList("Import files", "What are you importing?", new[] { "Characters (.zip, .rar or folders)", "Music (.ogg, .mp3)" },
            index => { if(index == 0) { StartCharacters(); } else { StartMusic(); } }, "Cancel", null);
    }

    private void StartCharacters()
    {
        music = false;
        picked.Clear();
        ignored = 0;
        dialog.ShowList("Import characters", "Pick .zip or .rar files, or folders. Only folders named CHAR_ are imported.",
            new[] { "Files (.zip, .rar)", "Char Folders" },
            index => { if(index == 0) { picker.PickFiles(false, OnPicked); } else { picker.PickFolders(OnPicked); } },
            "Cancel", null);
    }

    private void StartMusic()
    {
        music = true;
        picked.Clear();
        ignored = 0;
        picker.PickFiles(true, OnPicked);
    }

    private void OnPicked(IReadOnlyList<string> paths)
    {
        var supported = music ? ImportSources.SupportedAudio(picked.Concat(paths), out var unsupported) : ImportSources.Supported(picked.Concat(paths), out unsupported);
        picked.Clear();
        picked.AddRange(supported);
        ignored += unsupported;

        if(picked.Count == 0)
        {
            dialog.Show("Nothing to import", music ? "None of the picked items is an .ogg or .mp3 file." : "None of the picked items is a .zip, a .rar or a folder.", "OK", null);
            return;
        }
        Confirm();
    }

    private void Confirm()
    {
        var text = music ? ImportSources.ConfirmText(picked, ignored, 5, "not .ogg or .mp3") : ImportSources.ConfirmText(picked, ignored);
        if(picker.OneFolderAtATime && picked.Count > 0 && ImportSources.Classify(picked[0]) == ImportSourceKind.Folder)
        {
            dialog.ShowList(music ? "Import music" : "Import characters", text, new[] { "Import", "Add another folder" },
                index => { if(index == 0) { Begin(); } else { picker.PickFolders(OnPicked); } }, "Cancel", () => Cleanup());
            return;
        }
        dialog.Show(music ? "Import music" : "Import characters", text, "Import", Begin, "Cancel", () => Cleanup());
    }

    private void Begin()
    {
        var cacheDir = Path.Combine(Application.persistentDataPath, "cache");
        var temp = picker.TakeTempDirs();
        var modsDir = music ? Path.Combine(GameDataManager.GM.GetModsPath(), "music") : GameDataManager.GM.GetModsPath();
        if(!DownloadManager.EnqueueImport(picked.ToList(), modsDir, cacheDir, temp, music))
        {
            dialog.Show("Import files", "Another import is already running.", "OK", null);
            Cleanup(temp);
        }
    }

    private void Cleanup(IReadOnlyList<string> dirs = null)
    {
        foreach(var dir in dirs ?? picker.TakeTempDirs())
        {
            try { if(Directory.Exists(dir)) { Directory.Delete(dir, true); } }
            catch(IOException) { }
            catch(System.UnauthorizedAccessException) { }
        }
    }
}
