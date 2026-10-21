using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class SpriteLibrary : MonoBehaviour
{
    public List<Sprite> SpriteList;

    public static SpriteLibrary SL;

    void Awake()
    {
        //Singleton pattern
        if (SL == null) {
            SL = this;
        }
        else if (SL != this) {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(this);
    }

    //Files the system refused to let the app read this session (shown to the player by the menu)
    public static readonly List<string> Unreadable = new List<string>();

    public void AddExternalSprite(string filePath, string spriteName)
    {
        if(string.IsNullOrEmpty(filePath) || !File.Exists(filePath) || SpriteList.Any(x => x.name == spriteName)) { return; }

        byte[] fileData;
        try { fileData = File.ReadAllBytes(filePath); }
        catch(System.Exception e) when(e is System.UnauthorizedAccessException || e is System.IO.IOException)
        {
            //One file the app may not read must not stop the rest of the menu from loading
            Debug.LogWarning("Could not read " + filePath + ": " + e.Message);
            if(!Unreadable.Contains(filePath)) { Unreadable.Add(filePath); }
            return;
        }

        var spriteTexture = new Texture2D(1, 1);
        if(spriteTexture.LoadImage(fileData))
        {
            var sprite = Sprite.Create(spriteTexture, new Rect(0, 0, spriteTexture.width, spriteTexture.height), Vector2.zero, 100);
            sprite.name = spriteName;
            SpriteList.Add(sprite);
        }
    }

    public void RemoveSpritesByName(IEnumerable<string> names)
    {
        SpriteList.RemoveAll(x => names.Contains(x.name));
    }

    public Sprite GetSpriteByName(string name)
    {
        return SpriteList.Find(x => x.name == name);
    }
}