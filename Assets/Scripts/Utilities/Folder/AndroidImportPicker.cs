#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

//Android's system picker (Storage Access Framework) through a small Java plugin that copies the picked
//content into the app's cache, so the importer reads ordinary files. No storage permission is needed.
public class AndroidImportPicker : MonoBehaviour, IImportPicker
{
    private Action<IReadOnlyList<string>> pendingCallback;
    private readonly List<string> tempDirs = new List<string>();

    public string UnavailableReason => null;
    public bool OneFolderAtATime => true;

    private void Awake()
    {
        //Copies left behind by a previous run (killed app) are not needed any more
        try
        {
            var stale = Path.Combine(Application.temporaryCachePath, "import");
            if(Directory.Exists(stale)) { Directory.Delete(stale, true); }
        }
        catch(Exception) { }
    }

    public IReadOnlyList<string> TakeTempDirs()
    {
        var result = new List<string>(tempDirs);
        tempDirs.Clear();
        return result;
    }

    public void PickFiles(bool audio, Action<IReadOnlyList<string>> onPicked) { Launch(audio ? "pickAudio" : "pickFiles", onPicked); }
    public void PickFolders(Action<IReadOnlyList<string>> onPicked) { Launch("pickFolder", onPicked); }

    private void Launch(string method, Action<IReadOnlyList<string>> onPicked)
    {
        pendingCallback = onPicked;
        try
        {
            using(var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using(var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
            using(var picker = new AndroidJavaClass("com.super4p.picker.S4PPicker"))
            {
                picker.CallStatic(method, activity);
            }
        }
        catch(Exception e) { Debug.LogWarning("Could not open the picker: " + e.Message); pendingCallback = null; }
    }

    //Called by the Java plugin through UnitySendMessage
    public void OnPicked(string message)
    {
        var result = PickerOutput.ParseAndroidResult(message);
        tempDirs.AddRange(result.TempDirs);
        var callback = pendingCallback;
        pendingCallback = null;
        if(result.Failed.Count > 0)
        {
            var dialog = FindObjectOfType<GetModsPanel>()?.Dialog;
            dialog?.Show("Could not read some items", string.Join("\n", result.Failed) + "\n\nThe other items can still be imported.", "OK", null);
        }
        if(result.Paths.Count > 0) { callback?.Invoke(result.Paths); }
    }
}
#endif
