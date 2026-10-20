using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Android;

public class Logger : MonoBehaviour 
{
    private readonly string LOG_DIR = "/../logs";
    private string logDirPath;
    private readonly int LOG_LIMIT = 150;
    private string logFile;
    private bool HasCheckedLimit;
    public static Logger LG;
    
    void Awake() {
        //Singleton pattern
        if (LG == null) { LG = this; }
        else if (LG != this) { Destroy(gameObject); }
        DontDestroyOnLoad(this);
    }

    public void Initialize()
    {
        //macOS keeps logs outside the app bundle (a signed bundle must not be written to)
        #if UNITY_ANDROID && !UNITY_EDITOR
            //Next to the mods folder in the app's media folder, so the player can find the log in the Files app
            logDirPath = (AndroidFiles.MediaDir() ?? Application.persistentDataPath) + "/logs";
        #elif UNITY_STANDALONE_OSX || UNITY_IOS
            logDirPath = Application.persistentDataPath + "/logs";
        #else
            logDirPath = Application.dataPath + LOG_DIR;
        #endif

        //Create the log folder; a folder that cannot be created only means no log file
        try { Directory.CreateDirectory(logDirPath); }
        catch(Exception e) { UnityEngine.Debug.LogWarning("Could not create the log folder " + logDirPath + ": " + e.Message); }
        Application.logMessageReceived += WriteToLog;
    }

    //Write directly to log file (create if none so far)
    public void WriteToLog(string logString, string stackTrace, LogType type)
    {
        if(!HasCheckedLimit) {
            var allLogs = Directory.GetFiles(logDirPath);
            if(allLogs.Length >= LOG_LIMIT) { File.Delete(allLogs.First()); }
            HasCheckedLimit = true;
        }

        if(logFile == null) { logFile = logDirPath + "/" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt"; }

        File.AppendAllText(logFile, 
            Enum.GetName(typeof(LogType), type) + logString + "\r\n" + 
            stackTrace + 
            "------------------------------------------------------------\r\n");
    }
}