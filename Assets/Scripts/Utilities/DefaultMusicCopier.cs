using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

//Phones: the app carries a default music track (StreamingAssets/DefaultMusic, added at build time). The first time the app runs,
//it is copied into the music folder. Files inside an APK cannot be listed, so a small list.txt names them.
public static class DefaultMusicCopier
{
    private const string Folder = "DefaultMusic";

    public static IEnumerator CopyOnce(Action onCopied)
    {
        var config = GameDataManager.GM.PlyrConfigData;
        if(config.DefaultMusicCopied) { yield break; }

        //Android already gives a jar:file:// address; iPhone gives a plain path that needs file://
        var basePath = Application.streamingAssetsPath;
        if(!basePath.Contains("://")) { basePath = "file://" + basePath; }
        var root = basePath + "/" + Folder + "/";
        string[] names;
        using(var list = UnityWebRequest.Get(root + "list.txt"))
        {
            yield return list.SendWebRequest();
            if(list.result != UnityWebRequest.Result.Success) { yield break; }   //this build carries no default music
            names = list.downloadHandler.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(n => n.Trim()).Where(n => n.EndsWith(".ogg")).ToArray();
        }

        var musicDir = Path.Combine(GameDataManager.GM.GetModsPath(), "music");
        var copied = 0;
        foreach(var name in names)
        {
            string target;
            if(!FlatTarget.TryResolve(musicDir, name, out target) || File.Exists(target)) { continue; }

            using(var file = UnityWebRequest.Get(root + Uri.EscapeDataString(name)))
            {
                yield return file.SendWebRequest();
                if(file.result != UnityWebRequest.Result.Success) { Debug.LogWarning("Default music " + name + " could not be read: " + file.error); continue; }
                try
                {
                    Directory.CreateDirectory(musicDir);
                    File.WriteAllBytes(target, file.downloadHandler.data);
                    copied++;
                }
                catch(Exception e) when(e is IOException || e is UnauthorizedAccessException) { Debug.LogWarning("Default music " + name + " could not be saved: " + e.Message); }
            }
        }

        config.DefaultMusicCopied = true;
        GameDataManager.GM.SavePlyrCfgData();
        if(copied > 0) { onCopied?.Invoke(); }
    }
}
