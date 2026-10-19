using System;
using UnityEngine;

//Android storage helpers. The mods folder lives in the app's "media" folder (Android/media/<package>): the Files app shows it
//and the app can use it with no storage permission, unlike Android/data which newer Android versions hide.
public static class AndroidFiles
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject Activity()
    {
        using(var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) { return player.GetStatic<AndroidJavaObject>("currentActivity"); }
    }
#endif

    //The app's media folder on shared storage, or null when Android does not provide one
    public static string MediaDir()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using(var activity = Activity())
            {
                foreach(var dir in activity.Call<AndroidJavaObject[]>("getExternalMediaDirs"))
                {
                    if(dir == null) { continue; }
                    var path = dir.Call<string>("getAbsolutePath");
                    if(!string.IsNullOrEmpty(path)) { return path; }
                }
            }
        }
        catch(Exception e) { Debug.LogWarning("Could not find the app media folder: " + e.Message); }
#endif
        return null;
    }

    //Asks Android to show a folder in the Files app; false when no app can show it
    public static bool OpenFolder(string path)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        var uri = AndroidFolderLink.DocumentUri(path);
        if(uri == null) { return false; }
        try
        {
            using(var activity = Activity())
            using(var parsed = new AndroidJavaClass("android.net.Uri").CallStatic<AndroidJavaObject>("parse", uri))
            using(var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.VIEW"))
            {
                intent.Call<AndroidJavaObject>("setDataAndType", parsed, "vnd.android.document/directory");
                intent.Call<AndroidJavaObject>("addFlags", 0x10000000);   //FLAG_ACTIVITY_NEW_TASK
                activity.Call("startActivity", intent);
                return true;
            }
        }
        catch(Exception e) { Debug.LogWarning("Could not open the folder in Files: " + e.Message); }
#endif
        return false;
    }
}
