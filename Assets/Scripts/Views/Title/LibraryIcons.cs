using System.Collections.Generic;
using System.IO;
using UnityEngine;

//Series logos from the library sheet, loaded from the extracted files and shrunk (500 px pictures would be heavy on phones)
public static class LibraryIcons
{
    private const int Size = 128;
    private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();
    private static Font lobster;

    //The font the sheet uses for the letters shown instead of a logo
    public static Font Lobster => lobster != null ? lobster : (lobster = Resources.Load<Font>("Fonts/Lobster-Regular"));

    //Null when the file is missing or not a picture
    public static Texture2D Load(string path)
    {
        if(string.IsNullOrEmpty(path)) { return null; }
        if(Cache.TryGetValue(path, out var cached) && cached != null) { return cached; }
        if(!File.Exists(path)) { return null; }

        var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if(!source.LoadImage(File.ReadAllBytes(path))) { return null; }
            var target = RenderTexture.GetTemporary(Size, Size, 0, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            Graphics.Blit(source, target);
            RenderTexture.active = target;
            var small = new Texture2D(Size, Size, TextureFormat.RGB24, false);
            small.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            small.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            Cache[path] = small;
            return small;
        }
        catch(IOException) { return null; }
        catch(System.UnauthorizedAccessException) { return null; }
        finally { Object.Destroy(source); }
    }
}
