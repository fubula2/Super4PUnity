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

    //Files the app was opened with (copied to the cache by the plugin); the menu takes them and starts the import flow
    public static readonly System.Collections.Concurrent.ConcurrentQueue<IReadOnlyList<string>> Opened = new System.Collections.Concurrent.ConcurrentQueue<IReadOnlyList<string>>();

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

    //Asks the plugin whether the app was opened with a .zip/.rar; the answer arrives in OnOpened
    public void CheckOpenedFile()
    {
        try
        {
            using(var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using(var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
            using(var picker = new AndroidJavaClass("com.super4p.picker.S4PPicker"))
            {
                picker.CallStatic("handleIntent", activity);
            }
        }
        catch(Exception e) { Debug.LogWarning("Could not check the opened file: " + e.Message); }
    }

    //Called by the Java plugin through UnitySendMessage
    public void OnOpened(string message)
    {
        var result = PickerOutput.ParseAndroidResult(message);
        tempDirs.AddRange(result.TempDirs);
        if(result.Failed.Count > 0) { ShowFailed(result.Failed); }
        if(result.Paths.Count > 0) { Opened.Enqueue(result.Paths); }
    }

    private static void ShowFailed(IReadOnlyList<string> failed)
    {
        var dialog = FindObjectOfType<GetModsPanel>()?.Dialog;
        dialog?.Show("Could not read some items", string.Join("\n", failed) + "\n\nThe other items can still be imported.", "OK", null);
    }

    //Called by the Java plugin through UnitySendMessage
    public void OnPicked(string message)
    {
        var result = PickerOutput.ParseAndroidResult(message);
        tempDirs.AddRange(result.TempDirs);
        var callback = pendingCallback;
        pendingCallback = null;
        if(result.Failed.Count > 0) { ShowFailed(result.Failed); }
        if(result.Paths.Count > 0) { callback?.Invoke(result.Paths); }
    }
}
#endif
