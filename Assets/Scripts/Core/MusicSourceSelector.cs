using System;
using System.Collections.Generic;

public static class MusicSourceSelector
{
    //The provider that handles this link, or null when none does
    public static IMusicSource ForUrl(string url, IEnumerable<IMusicSource> sources)
    {
        if(!Uri.TryCreate(url, UriKind.Absolute, out var uri)) { return null; }
        foreach(var s in sources) { if(s.Handles(uri)) { return s; } }
        return null;
    }
}
