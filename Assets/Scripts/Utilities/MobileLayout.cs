using System;
using UnityEngine;

//Phones and tablets: no desktop window options, a screen shape that is not 16:9, and touch instead of keys.
public static class MobileLayout
{
    public static bool Active
    {
        get
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if(Environment.GetEnvironmentVariable("S4P_FORCE_MOBILE_LAYOUT") == "1") { return true; }
#endif
            return Application.platform == RuntimePlatform.Android;
        }
    }
}
