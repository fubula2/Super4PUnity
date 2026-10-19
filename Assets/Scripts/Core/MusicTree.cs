using System;
using System.Collections.Generic;
using System.Linq;

public sealed class MusicNode
{
    public MusicEntry Entry;
    public bool Expanded;
    public bool Loaded;
    public string Error;
    public List<MusicNode> Children = new List<MusicNode>();

    public int FileCount
    {
        get
        {
            if(Entry != null && Entry.Kind == MusicEntryKind.File) { return 1; }
            return Children.Sum(c => c.FileCount);
        }
    }
}

//Lazily loaded folder tree: folders first, then .ogg files, each sorted by name.
public sealed class MusicTree
{
    public readonly MusicNode Root = new MusicNode { Entry = new MusicEntry { Id = "", Name = "", Kind = MusicEntryKind.Folder }, Expanded = true };

    public void SetChildren(MusicNode node, IEnumerable<MusicEntry> entries)
    {
        var kept = entries.Where(e => e.Kind == MusicEntryKind.Folder || e.Name.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase));
        node.Children = kept
            .OrderBy(e => e.Kind == MusicEntryKind.Folder ? 0 : 1)
            .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .Select(e => new MusicNode { Entry = e })
            .ToList();
        node.Loaded = true;
        node.Error = null;
    }
}
