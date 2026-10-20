using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif

//Idea 1: Refresh functionality? On Title Screen click to reload mods, presets, etc...
//Refactor 1: Remove ParsedPresetData and func to separate data manager (only on title)
public class GameDataManager : MonoBehaviour {

    public PlayerConfigData PlyrConfigData;
    public Dictionary<string, string> ParsedPresetData;

    private string PLAYER_DIR = "/config";
    private string MODS_DIR;
    private string MUSIC_DIR;

    private readonly string PLAYERCONFIG_PATH = "/playerConfig.json";
    private readonly string PRESET_PATH = "/presets.json";

    public List<string> CharDirs;
    public List<string> SelectedMods;
    public List<string> SelectedMusic;

    public static bool IsPlayerDataLoaded;

    public static GameDataManager GM;
    
    void Awake() {
        //Singleton pattern
        if (GM == null) { GM = this; }
        else if (GM != this) { Destroy(gameObject); }
        DontDestroyOnLoad(this);
    }

    public void Initialize()
    {
        if(IsPlayerDataLoaded) { return; }
        
        //Set the config directory (for rest of runtime); mods/music are resolved once config is loaded
        #if UNITY_STANDALONE_OSX || UNITY_IOS
            PLAYER_DIR = Application.persistentDataPath + PLAYER_DIR;
        #elif UNITY_ANDROID
            PLAYER_DIR = Application.persistentDataPath + PLAYER_DIR;

            if(!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead)) {
                Permission.RequestUserPermission(Permission.ExternalStorageRead);
            }
            if(!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite)) {
                Permission.RequestUserPermission(Permission.ExternalStorageWrite);
            }
        #else
            PLAYER_DIR = Application.dataPath + "/.." + PLAYER_DIR;
        #endif 
    
        var playerConfigFilePath = PLAYER_DIR + PLAYERCONFIG_PATH;

        if(!DataUtility.CheckDirectoryExists(PLAYER_DIR)) {  Directory.CreateDirectory(PLAYER_DIR); }

        if(!DataUtility.CheckFileExists(playerConfigFilePath, false))
        {
            PlyrConfigData = CreateInitialPlyrCfgData();
            SavePlyrCfgData();
        }
        else { 
            PlyrConfigData = DataUtility.LoadFromJSON<PlayerConfigData>(playerConfigFilePath, false); 
            ValidatePlyrCfgData();
        }

        ApplyModsPaths();
        LoadCharDirs();

        // else
        // {
        //     if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite) && !Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
        //     {
        //         // Ask for permission or proceed without the functionality enabled.
        //         Permission.RequestUserPermission(Permission.ExternalStorageRead);
        //         Permission.RequestUserPermission(Permission.ExternalStorageWrite);
        //     }   
        //     if(!Directory.Exists(GameDataManager.GM.GetAndroidInternalFilesDir() + "/.4PUnity_V2")) {
        //         Directory.CreateDirectory(GameDataManager.GM.GetAndroidInternalFilesDir() + "/.4PUnity_V2");
        //     }
        // }

        IsPlayerDataLoaded = true;
    }
    public event Action ModsPathChanged;
    public bool ModsPathFellBack { get; private set; }

    //The app and its mods folder always live in the same folder. The folder is created (with music and voices) when it
    //is missing; only if that is impossible (for example a read-only location) does the per-user folder take over.
    private string DefaultModsPath()
    {
        var perUser = Application.persistentDataPath + "/mods";
        #if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            var next = MacAppLocation.ModsFolderNextToApp(MacAppBundlePath());
            return next != null && EnsureModsFolder(next) ? next : perUser;
        #elif UNITY_STANDALONE && !UNITY_EDITOR
            var next = Application.dataPath + "/../mods";
            return EnsureModsFolder(next) ? next : perUser;
        #elif UNITY_ANDROID
            var media = AndroidFiles.MediaDir();
            var androidMods = media != null ? media + "/mods" : perUser;
            EnsureModsFolder(androidMods);
            return androidMods;
        #elif UNITY_IOS
            //The app's Documents folder: with file sharing switched on (see IosBuildSettings) it shows up in the Files app
            EnsureModsFolder(perUser);
            return perUser;
        #elif UNITY_STANDALONE_OSX
            return perUser;
        #else
            return Application.dataPath + "/../mods";
        #endif
    }

    private static bool EnsureModsFolder(string path)
    {
        try
        {
            if(Directory.Exists(path)) { return true; }
            Directory.CreateDirectory(path);
            Directory.CreateDirectory(path + "/music");
            Directory.CreateDirectory(path + "/voices");
            return true;
        }
        catch(Exception e)
        {
            Debug.LogWarning("Could not create the mods folder next to the app (" + path + "): " + e.Message);
            return false;
        }
    }

    #if UNITY_STANDALONE_OSX && !UNITY_EDITOR
    //The .app's real location, looking through macOS "app translocation" (a randomized read-only copy used for downloaded apps)
    private static string MacAppBundlePath()
    {
        var bundle = Path.GetDirectoryName(Application.dataPath);   //dataPath is <bundle>/Contents
        if(!MacAppLocation.IsTranslocated(bundle)) { return bundle; }

        try
        {
            var info = new System.Diagnostics.ProcessStartInfo("/sbin/mount") { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
            using(var process = System.Diagnostics.Process.Start(info))
            {
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(3000);
                return MacAppLocation.OriginalPathFromMount(output, bundle);
            }
        }
        catch(Exception) { return null; }
    }
    #endif

    private void ApplyModsPaths()
    {
        var paths = ModsPathResolver.Resolve(PlyrConfigData.ModsPath, DefaultModsPath(), Directory.Exists);
        MODS_DIR = paths.Mods;
        MUSIC_DIR = paths.Music;
        ModsPathFellBack = paths.UsedFallback;
        Debug.Log("Mods folder: " + MODS_DIR);
        if(paths.UsedFallback) { Debug.LogWarning("Configured mods folder not found, using default: " + MODS_DIR); }
    }

    private void LoadCharDirs()
    {
        try
        {
            CharDirs = Directory.GetDirectories(MODS_DIR, "CHAR_*").ToList();
        }
        catch(Exception)
        {
            CharDirs = new List<string>();
            Debug.LogWarning("Mods folder not found (char search): " + MODS_DIR);
        }
    }

    //Persist a newly chosen mods folder and reload everything that depends on it
    public void SetModsPath(string path)
    {
        PlyrConfigData.ModsPath = ModsPathResolver.Normalize(path);
        SavePlyrCfgData();
        RefreshMods();
    }

    //Re-read the mods folder without touching the current selection (used after background installs)
    public void ReloadCharDirsOnly()
    {
        ApplyModsPaths();
        LoadCharDirs();
    }

    //Re-read mod folders and music from the current mods folder
    public void RefreshMods()
    {
        ApplyModsPaths();
        LoadCharDirs();
        SelectedMods = new List<string>();
        SelectedMusic = new List<string>();
        ModsPathChanged?.Invoke();
    }

    public string GetMusicPath(string musicName)
    {
        return MUSIC_DIR + "/" + musicName;
    }

    public List<string> GetMusicFilePaths()
    {
        var musicFilePaths = new List<string>();

        try
        {
            musicFilePaths.AddRange(Directory.GetFiles(MUSIC_DIR).Where(MediaFiles.IsMusic));
        }
        catch(Exception)
        {
            Debug.LogWarning("Mods folder not found (music search): " + MODS_DIR);
        }
        return musicFilePaths;
    }

    //Set Music based on results of Title Screen
    public void SetSelectedMusic(string selectedMusicStr)
    {
        SelectedMusic = selectedMusicStr.Split(',').OrderBy(x => x).ToList();

        if(SelectedMusic.Any(x => x.Contains("BBSa_")))
        {
            var prioritizedMusic = SelectedMusic.Where(x => x.Contains("BBSa_")).ToList();
            foreach(var music in prioritizedMusic)
            {
                SelectedMusic.Remove(music);
            }
            SelectedMusic.InsertRange(0, prioritizedMusic);
        }
    }

    //Set Mods based on results of Title Screen
    public void SetSelectedMods(string selectedModStr)
    {
        SelectedMods = selectedModStr.Split(',').OrderBy(x => x).ToList();
    }

    public string GetModsPath()
    {
        return MODS_DIR;
    }
    public string GetMusicPath()
    {
        return MUSIC_DIR;
    }

    public List<string> GetVideoPaths(string modName)
    {
        return MediaFiles.VideosIn(MODS_DIR + "/CHAR_" + modName);
    }

    public void LoadPresetData()
    {
        ParsedPresetData = new Dictionary<string, string>();
        var playerPresetPath = PLAYER_DIR + PRESET_PATH;

        if(!DataUtility.CheckFileExists(playerPresetPath, false)) {
            SavePresetData();
        }
        else { 
            //TODO: Don't store this permanently, not needed
            var presetData = DataUtility.LoadFromJSON<PresetData>(playerPresetPath, false); 
            if(string.IsNullOrWhiteSpace(presetData.Presets)) { return; }
            
            foreach(var preset in presetData.Presets.Split('|'))
            {
                var innerPreset = preset.Split('-');
                var presetKey = innerPreset[0].Trim();
                var presetValue = innerPreset[1].Trim();

                ParsedPresetData.Add(presetKey, presetValue);
            }
        }
    }

    public void SavePresetData()
    {
        var presetData = new PresetData();
        bool isFirstIteration = true;

        foreach(var key in ParsedPresetData.Keys)
        {
            if(!isFirstIteration) { presetData.Presets += "|"; }
            presetData.Presets += key + "-" + ParsedPresetData[key];

            if(isFirstIteration) { isFirstIteration = false; }
        }

        DataUtility.SaveToJSON(PLAYER_DIR + PRESET_PATH, presetData, false);
    }

    public void AddPresetData(string presetName, string modListStr, string musicListStr)
    {
        if(ParsedPresetData.ContainsKey(presetName)) { ParsedPresetData[presetName] = modListStr; }
        else { ParsedPresetData.Add(presetName, modListStr + '>' + musicListStr); }

        SavePresetData();
    }

    public void DeletePresetData(string presetName)
    {
        ParsedPresetData.Remove(presetName);
        SavePresetData();
    }

    public void SavePlyrCfgData()
    {
        DataUtility.SaveToJSON(PLAYER_DIR + PLAYERCONFIG_PATH, PlyrConfigData, false);
    }

    private PlayerConfigData CreateInitialPlyrCfgData()
    {
        return new PlayerConfigData()
        {
            AspectResolution = AspectResolution.SevenTwenty,
            IsFullscreen = false,
            MusicVolume = 1f,
            VideoVolume = 1f,
            VoiceVolume = 1f,
            CharPlaybackStyle = PlaybackStyle.Loop,
            PosePlaybackStyle = PlaybackStyle.Loop,
            Version = Application.version
        };
    }
    
    //Check if PlyrConfigData has null values, if so
    private void ValidatePlyrCfgData()
    {
        PlayerConfigData initialPlyrConfig = null;

        foreach(var field in PlyrConfigData.GetType().GetFields())
        {
            if(field.GetValue(PlyrConfigData) == null) {
                if(initialPlyrConfig == null) {
                    initialPlyrConfig = CreateInitialPlyrCfgData();
                }
                field.SetValue(PlyrConfigData, field.GetValue(initialPlyrConfig));
            }
        }

        PlyrConfigData.Version = Application.version;
    }

    public string GetAndroidInternalFilesDir()
    {
        string[] potentialDirectories = new string[]
        {
            "/mnt/sdcard",
            "/sdcard",
            "/storage/sdcard0",
            "/storage/sdcard1",
            "/storage/emulated/0"
        };

        if (Application.platform == RuntimePlatform.Android)
        {
            for (int i = 0; i < potentialDirectories.Length; i++)
            {
                if (Directory.Exists(potentialDirectories[i]))
                {
                    return potentialDirectories[i];
                }
            }
        }
        return "";
    }   
}