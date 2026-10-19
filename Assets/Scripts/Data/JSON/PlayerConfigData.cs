using System;

[Serializable]
public class PlayerConfigData
{
    public AspectResolution AspectResolution = AspectResolution.SevenTwenty;
    public bool IsFullscreen = false;

    //30 or 60; 0 means not chosen yet (see FrameRatePolicy)
    public int TargetFps;
    
    public float MusicVolume = 1f;
    public float VideoVolume = 1f;
    public float VoiceVolume = 1f;

    public PlaybackStyle CharPlaybackStyle = PlaybackStyle.Loop;
    public PlaybackStyle PosePlaybackStyle = PlaybackStyle.Loop;

    //Empty means use the default mods location
    public string ModsPath;

    //Set once the player confirms the 18+ notice in Get Mods
    public bool AgeNoticeAccepted;

    //Get Mods sort order: 0 sheet, 1 newest first, 2 oldest first
    public int LibrarySort;

    //Update check: when it last ran (UTC, round-trip text) and a version the player chose not to be told about again
    public string LastUpdateCheckUtc;
    public string SkippedUpdateVersion;

    //Phones: the video fills the screen (pinch out) instead of keeping its shape with bars (pinch in)
    public bool VideoFillScreen;

    //Android: the default music track packaged in the app has been copied into the music folder
    public bool DefaultMusicCopied;

    public string Version;
}