//The frame rate the player chose (30 or 60). Nothing chosen yet: 60 everywhere (phones can still pick 30).
public static class FrameRatePolicy
{
    public static int Resolve(int saved, bool mobile)
    {
        if(saved == 30 || saved == 60) { return saved; }
        return 60;
    }

    public static int IndexOf(int fps) { return fps == 30 ? 0 : 1; }

    public static int FromIndex(int index) { return index == 0 ? 30 : 60; }
}
