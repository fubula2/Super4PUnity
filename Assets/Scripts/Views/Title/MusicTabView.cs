using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

//The Music tab of the Get Mods popup: collapsible folders of .ogg files from the sheet's music link.
//Rows are the same prefab as the Characters list. Downloads go through DownloadManager (flat into <mods>/music).
public class MusicTabView
{
    private const float RowHeight = 56f;
    private const float Indent = 34f;
    private const float NameLeft = 30f;
    private const float NameRight = 1228f - 20f;   //the action button starts at x 1228 in the row template
    private const int ParallelLoads = 6;
    private const float HudShare = 0.8f;           //the progress strip treats 0.8 as a finished download

    private readonly MonoBehaviour host;
    private readonly RectTransform content;
    private readonly LibraryRow template;
    private readonly Action<string> setStatus;
    private readonly string cacheDir;

    private readonly List<GameObject> rows = new List<GameObject>();
    private readonly List<(MusicNode node, LibraryRow row)> fileRows = new List<(MusicNode, LibraryRow)>();
    private readonly HashSet<MusicNode> loading = new HashSet<MusicNode>();
    private MusicService service;
    private MusicTree tree;
    private string linkUrl;
    private string query = string.Empty;
    private CancellationTokenSource cts;
    private bool loadingAll;
    private ScrollRect scrollRect;

    public MusicTabView(MonoBehaviour host, RectTransform content, LibraryRow template, Action<string> setStatus, string cacheDir)
    {
        this.host = host; this.content = content; this.template = template; this.setStatus = setStatus; this.cacheDir = cacheDir;
    }

    //Forgets loaded folders; the next Show fetches the listing again
    public void Reset() { Cancel(); service = null; tree = null; linkUrl = null; }

    public void Cancel() { cts?.Cancel(); cts = null; loading.Clear(); loadingAll = false; }

    public void Hide() { Cancel(); Clear(); }

    public void Show(LibraryCatalog catalog, string currentQuery)
    {
        query = currentQuery ?? string.Empty;
        var url = catalog?.MusicFolderUrl;
        if(string.IsNullOrEmpty(url)) { Clear(); setStatus("No music link was found in the library sheet."); return; }

        if(service == null || linkUrl != url)
        {
            Cancel();
            linkUrl = url;
            service = new MusicService(url);
            tree = new MusicTree();
        }
        if(!service.IsUsable) { Clear(); setStatus(service.Source == null ? "Music source not supported yet." : "The music link in the sheet is not a folder link."); return; }

        if(cts == null) { cts = new CancellationTokenSource(); }
        Rebuild();
        if(!tree.Root.Loaded) { LoadFolder(tree.Root); }
    }

    public void ApplyQuery(string text)
    {
        query = text ?? string.Empty;
        if(tree == null) { return; }
        Rebuild();
        if(!string.IsNullOrWhiteSpace(query)) { LoadAllFolders(); }
    }

    public void RefreshStates()
    {
        if(fileRows.Count == 0) { return; }
        var snap = DownloadManager.Snapshot;
        foreach(var (node, row) in fileRows)
        {
            var key = Key(node);
            if(snap.Active && snap.Key == key) { row.SetState(RowState.Active, Mathf.Clamp01(snap.Fraction / HudShare)); }
            else if(snap.QueuedKeys.Contains(key)) { row.SetState(RowState.Queued, 0f); }
            else { row.SetState(RowState.Ready, 0f); }
        }
    }

    //Used by the development probe
    public int RowCount => rows.Count;
    public int FileRowCount => fileRows.Count;
    public void ProbeToggleFirstFolder() { var f = tree?.Root.Children.FirstOrDefault(c => c.Entry.Kind == MusicEntryKind.Folder); if(f != null) { ToggleFolder(f); } }
    public void ProbeDownloadAll() { foreach(var (node, _) in fileRows.ToList()) { OnFileAction(node); } }
    public void ProbeDownloadFirst() { if(fileRows.Count > 0) { OnFileAction(fileRows[0].node); } }

    private static string Key(MusicNode node) { return "music:" + node.Entry.Id; }

    private async void LoadFolder(MusicNode node)
    {
        if(service == null || cts == null || !loading.Add(node)) { return; }
        var ct = cts.Token;
        node.Error = null;
        try
        {
            var id = node == tree.Root ? service.RootId : node.Entry.Id;
            var entries = await service.ListAsync(id, ct);
            if(ct.IsCancellationRequested) { return; }
            tree.SetChildren(node, entries);
        }
        catch(HttpRequestException e) { if(!ct.IsCancellationRequested) { node.Error = e.Message; } }
        catch(OperationCanceledException) { return; }
        finally { loading.Remove(node); }
        if(ct.IsCancellationRequested) { return; }
        Rebuild();
    }

    //Searching needs every folder's file names, so load the ones not fetched yet, a few at a time
    private async void LoadAllFolders()
    {
        if(loadingAll || cts == null) { return; }
        loadingAll = true;
        var ct = cts.Token;
        try
        {
            while(!ct.IsCancellationRequested && !string.IsNullOrWhiteSpace(query))
            {
                var batch = new List<MusicNode>();
                CollectUnloaded(tree.Root, batch, ParallelLoads);
                if(batch.Count == 0) { break; }
                foreach(var n in batch) { loading.Add(n); }

                var results = await Task.WhenAll(batch.Select(async n =>
                {
                    try { return (node: n, entries: await service.ListAsync(n.Entry.Id, ct), error: (string)null); }
                    catch(HttpRequestException e) { return (node: n, entries: (IReadOnlyList<MusicEntry>)null, error: e.Message); }
                }));
                foreach(var r in results)
                {
                    loading.Remove(r.node);
                    if(r.entries != null) { tree.SetChildren(r.node, r.entries); } else { r.node.Error = r.error; }
                }
                Rebuild();
            }
        }
        catch(OperationCanceledException) { }
        finally { loadingAll = false; }
    }

    private bool HasUnloaded()
    {
        var one = new List<MusicNode>();
        CollectUnloaded(tree.Root, one, 1);
        return one.Count > 0;
    }

    private void CollectUnloaded(MusicNode parent, List<MusicNode> found, int max)
    {
        foreach(var child in parent.Children)
        {
            if(found.Count >= max) { return; }
            if(child.Entry.Kind != MusicEntryKind.Folder) { continue; }
            if(!child.Loaded) { if(child.Error == null && !loading.Contains(child)) { found.Add(child); } continue; }
            CollectUnloaded(child, found, max);
        }
    }

    private void Clear()
    {
        foreach(var r in rows) { r.SetActive(false); UnityEngine.Object.Destroy(r); }
        rows.Clear();
        fileRows.Clear();
        content.sizeDelta = new Vector2(content.sizeDelta.x, 0);
    }

    private void Rebuild()
    {
        var top = scrollRect != null ? scrollRect.verticalNormalizedPosition : 1f;
        Clear();
        if(tree == null) { return; }

        var searching = !string.IsNullOrWhiteSpace(query);
        var visible = MusicFilter.Visible(tree.Root, query);
        var y = 0f;

        if(!tree.Root.Loaded)
        {
            setStatus(tree.Root.Error != null ? "Could not load the music list: " + tree.Root.Error + "\nPress Reload to try again." : "Loading the music list...");
            return;
        }

        void Add(MusicNode parent, int depth)
        {
            foreach(var child in parent.Children)
            {
                if(searching && !visible.Contains(child)) { continue; }
                if(child.Entry.Kind == MusicEntryKind.File) { AddFile(child, depth, ref y); continue; }

                AddFolder(child, depth, ref y);
                if(!(child.Expanded || searching)) { continue; }
                if(child.Loaded)
                {
                    if(child.Children.Count == 0) { AddInfo("(empty)", depth + 1, null, ref y); }
                    else { Add(child, depth + 1); }
                }
                else if(child.Error != null) { var captured = child; AddInfo("Could not load this folder (" + child.Error + "). Click to retry.", depth + 1, () => LoadFolder(captured), ref y); }
                else { AddInfo("Loading...", depth + 1, null, ref y); }
            }
        }
        Add(tree.Root, 0);

        content.sizeDelta = new Vector2(content.sizeDelta.x, y);
        if(scrollRect == null) { scrollRect = content.GetComponentInParent<ScrollRect>(); }
        if(scrollRect != null) { Canvas.ForceUpdateCanvases(); scrollRect.verticalNormalizedPosition = searching ? 1f : Mathf.Clamp01(top); }

        if(tree.Root.Children.Count == 0) { setStatus("This music folder is empty."); }
        else if(searching && visible.Count <= 1) { setStatus(HasUnloaded() || loadingAll ? "Searching..." : "No matches."); }
        else { setStatus("Click a folder to open or close it. Downloads are saved to your mods/music folder."); }
        RefreshStates();
    }

    private LibraryRow NewRow(string text, int depth, ref float y)
    {
        var row = UnityEngine.Object.Instantiate(template, content);
        row.gameObject.SetActive(true);
        row.CharactersText.gameObject.SetActive(false);
        row.UpdatedText.gameObject.SetActive(false);
        row.SeriesLink.enabled = false;

        var name = row.SeriesText;
        var rt = name.rectTransform;
        var left = NameLeft + depth * Indent;
        rt.anchoredPosition = new Vector2(left, rt.anchoredPosition.y);
        rt.sizeDelta = new Vector2(NameRight - left, rt.sizeDelta.y);
        name.horizontalOverflow = HorizontalWrapMode.Wrap;
        name.verticalOverflow = VerticalWrapMode.Truncate;
        name.resizeTextForBestFit = true;
        name.resizeTextMaxSize = Mathf.Max(name.fontSize, name.resizeTextMaxSize);
        name.resizeTextMinSize = 14;
        name.text = text;

        ((RectTransform)row.transform).anchoredPosition = new Vector2(0, -y);
        y += RowHeight;
        rows.Add(row.gameObject);
        return row;
    }

    private static void MakeClickable(LibraryRow row, Action onClick)
    {
        var image = row.gameObject.GetComponent<Image>() ?? row.gameObject.AddComponent<Image>();
        image.color = new Color(1, 1, 1, 0);
        var button = row.gameObject.GetComponent<Button>() ?? row.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() => onClick());
    }

    private void AddFolder(MusicNode node, int depth, ref float y)
    {
        var count = node.Loaded ? "  (" + node.FileCount + (node.FileCount == 1 ? " track)" : " tracks)") : string.Empty;
        var open = node.Expanded || !string.IsNullOrWhiteSpace(query);
        var row = NewRow((open ? "- " : "+ ") + node.Entry.Name + count, depth, ref y);
        row.ActionButton.gameObject.SetActive(false);
        MakeClickable(row, () => ToggleFolder(node));
    }

    private void ToggleFolder(MusicNode node)
    {
        node.Expanded = !node.Expanded;
        if(node.Expanded && !node.Loaded && node.Error == null) { LoadFolder(node); }
        Rebuild();
    }

    private void AddInfo(string text, int depth, Action onClick, ref float y)
    {
        var row = NewRow(text, depth, ref y);
        row.ActionButton.gameObject.SetActive(false);
        row.SeriesText.color = new Color(row.SeriesText.color.r, row.SeriesText.color.g, row.SeriesText.color.b, 0.7f);
        if(onClick != null) { MakeClickable(row, onClick); }
    }

    private void AddFile(MusicNode node, int depth, ref float y)
    {
        var row = NewRow(node.Entry.Name, depth, ref y);
        row.ActionButton.onClick.AddListener(() => OnFileAction(node));
        fileRows.Add((node, row));
    }

    private void OnFileAction(MusicNode node)
    {
        var key = Key(node);
        var snap = DownloadManager.Snapshot;
        if(snap.Active && snap.Key == key) { setStatus(node.Entry.Name + " is downloading. Use the bar under the title to cancel."); return; }
        if(snap.QueuedKeys.Contains(key))
        {
            DownloadManager.RemoveQueued(key);
            setStatus(node.Entry.Name + " removed from the queue.");
            RefreshStates();
            return;
        }

        var request = new DownloadRequest
        {
            Kind = DownloadKind.Music, Key = key, Title = node.Entry.Name,
            FileUrl = service.Source.FileUrl(node.Entry.Id), FileName = node.Entry.Name,
            ModsDir = Path.Combine(GameDataManager.GM.GetModsPath(), "music"), WorkDir = cacheDir
        };
        var added = DownloadManager.Enqueue(request);
        setStatus(added ? node.Entry.Name + " added to downloads. You can close this window." : node.Entry.Name + " is already downloading or queued.");
        RefreshStates();
    }
}
