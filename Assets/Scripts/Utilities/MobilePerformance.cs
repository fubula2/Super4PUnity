using UnityEngine;

//Frame-rate cap (the player's 30/60 choice) and, on phones, no screen sleep while the app is open.
public static class MobilePerformance
{
    public static int CurrentFps => FrameRatePolicy.Resolve(GameDataManager.GM.PlyrConfigData.TargetFps, MobileLayout.Active);

    public static void ApplyFrameRate()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = CurrentFps;
    }

    public static void Apply()
    {
        ApplyFrameRate();
        if(MobileLayout.Active) { Screen.sleepTimeout = SleepTimeout.NeverSleep; }
    }
}
