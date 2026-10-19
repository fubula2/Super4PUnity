using System.Collections.Generic;

public static class MusicFilter
{
    //Nodes to show for a query. Empty query: every node. Otherwise matching files plus their ancestor folders (root included).
    public static HashSet<MusicNode> Visible(MusicNode root, string query)
    {
        var result = new HashSet<MusicNode>();
        var all = string.IsNullOrWhiteSpace(query);
        Walk(root, query, all, result);
        return result;
    }

    private static bool Walk(MusicNode node, string query, bool all, HashSet<MusicNode> result)
    {
        var show = all;
        if(node.Entry != null && node.Entry.Kind == MusicEntryKind.File && ModFilter.Matches(node.Entry.Name, query)) { show = true; }
        foreach(var child in node.Children) { if(Walk(child, query, all, result)) { show = true; } }
        if(show) { result.Add(node); }
        return show;
    }
}
