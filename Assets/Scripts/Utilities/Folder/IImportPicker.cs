using System;
using System.Collections.Generic;
using UnityEngine;

public interface IImportPicker
{
    //Null when the picker works; otherwise text to show the player (e.g. a missing helper on Linux)
    string UnavailableReason { get; }

    //True when only one folder can be chosen per dialog (the flow then offers "Add more")
    bool OneFolderAtATime { get; }

    //Callbacks run on the main thread with local paths; they are not called on cancel or failure
    //Temporary copies the picker made (Android); the importer deletes them when it is done. Clears the list.
    IReadOnlyList<string> TakeTempDirs();

    //audio: music files (.ogg, .mp3) instead of .zip/.rar archives
    void PickFiles(bool audio, Action<IReadOnlyList<string>> onPicked);
    void PickFolders(Action<IReadOnlyList<string>> onPicked);
}

public static class ImportPickers
{
    public static IImportPicker Create(GameObject host)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
            //The Java plugin finds this object by name, so it gets its own
            var receiver = GameObject.Find("ImportPickerReceiver") ?? new GameObject("ImportPickerReceiver");
            return receiver.GetComponent<AndroidImportPicker>() ?? receiver.AddComponent<AndroidImportPicker>();
        #elif UNITY_STANDALONE || UNITY_EDITOR
            return host.GetComponent<DesktopImportPicker>() ?? host.AddComponent<DesktopImportPicker>();
        #else
            return null;
        #endif
    }
}
