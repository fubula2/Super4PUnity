using System;

public interface IFolderPicker
{
    //onPicked runs on the main thread with the chosen folder; it is not called on cancel or failure
    void Pick(string title, string startPath, Action<string> onPicked);
}
