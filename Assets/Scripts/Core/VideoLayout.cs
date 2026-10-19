public enum VideoFit
{
    Height,   //full screen height, width from the picture's shape (what computers always did)
    Whole,    //the whole picture, centred, with bars where it does not reach the screen edges
    Fill      //the picture covers the whole screen, cropped where it overflows
}

public static class VideoLayout
{
    //Size to draw the picture at, for a screen of screenWidth x screenHeight; always centred by the caller
    public static (float Width, float Height) SizeFor(float screenWidth, float screenHeight, float aspect, VideoFit fit)
    {
        if(!(aspect > 0f) || !(screenWidth > 0f) || !(screenHeight > 0f)) { return (screenWidth, screenHeight); }

        float width;
        switch(fit)
        {
            case VideoFit.Whole: width = System.Math.Min(screenWidth, screenHeight * aspect); break;
            case VideoFit.Fill: width = System.Math.Max(screenWidth, screenHeight * aspect); break;
            default: width = screenHeight * aspect; break;
        }
        return (width, width / aspect);
    }
}
