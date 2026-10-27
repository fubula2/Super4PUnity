using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

//Title-screen browser for the online mod library. Downloads run in DownloadManager, so closing this panel
//never interrupts them; rows only show their state.
public class GetModsPanel : MonoBehaviour
{
    public GameObject Root;
    public InputField SearchInput;
    public Button SortBtn;
    public Text SortLabel;
    public Button ReloadBtn;
    public Button CloseBtn;
    public Text StatusText;
    public RectTransform Content;
    public LibraryRow RowTemplate;
    public Text HeaderTemplate;
    public Dialog Dialog;

    private const string Hint = "Tip: double-click a series name to open its page in your browser.";
    private const float RowHeight = 56f;
    private const float HeaderHeight = 60f;
    private const float StateRefreshInterval = 0.25f;

    private class Item
    {
        public GameObject Object;
        public LibraryRow Row;
        public int Group;
        public bool IsHeader;
    }

    private readonly List<Item> items = new List<Item>();
    private readonly Queue<LibraryRow> iconQueue = new Queue<LibraryRow>();   //rows waiting for their logo (a few per frame)
    private LibraryService service;
    private LibraryCatalog catalog;
    private CancellationTokenSource loadCts;
    private string cacheDir;
    private ScrollRect scrollRect;
    private float stateTimer;
    private SortMode sortMode;
    private MusicTabView music;
    private bool musicTab;
    private Button charsTabBtn;
    private Button musicTabBtn;
    private Text[] columnHeaders;
    private string[] columnTexts;
    private Color tabColor;

    public bool IsOpen => Root != null && Root.activeSelf;
    public bool IsTyping => SearchInput != null && SearchInput.isFocused;
    public int VisibleRowCount => items.Count(i => !i.IsHeader && i.Object.activeSelf);
    public SortMode CurrentSort => sortMode;
    public MusicTabView Music => music;
    public void SelectMusicTab() { SetTab(true); }
    public void SelectCharsTab() { SetTab(false); }

    private void Awake()
    {
        CloseBtn.onClick.AddListener(Close);
        ReloadBtn.onClick.AddListener(() => Load(true));
        SortBtn.onClick.AddListener(CycleSort);
        SearchInput.onValueChanged.AddListener(ApplyFilter);
        RowTemplate.gameObject.SetActive(false);
        HeaderTemplate.gameObject.SetActive(false);
        BuildTabs();
        StyleCloseButton();
        Root.SetActive(false);
    }

    //Tab buttons are cloned from the Sort button; the header row is re-spaced to make room
    private void BuildTabs()
    {
        var window = (RectTransform)SortBtn.transform.parent;
        void Place(RectTransform rt, float x, float width) { rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y); rt.sizeDelta = new Vector2(width, rt.sizeDelta.y); }
        var title = window.Find("TitleText").GetComponent<Text>();
        Place(title.rectTransform, 30f, 240f);
        title.fontSize = Mathf.RoundToInt(title.fontSize * 0.8f);
        title.horizontalOverflow = HorizontalWrapMode.Overflow;
        Place((RectTransform)SearchInput.transform, 610f, 325f);
        Place((RectTransform)SortBtn.transform, 945f, 220f);
        SortLabel.resizeTextForBestFit = true;
        SortLabel.resizeTextMaxSize = SortLabel.fontSize;
        SortLabel.resizeTextMinSize = 16;
        tabColor = SortBtn.GetComponent<Image>().color;

        Button Clone(string name, string label, float x, float width, Action onClick)
        {
            var b = Instantiate(SortBtn, window);
            b.name = name;
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() => onClick());
            Place((RectTransform)b.transform, x, width);
            b.GetComponentInChildren<Text>().text = label;
            b.gameObject.SetActive(true);
            return b;
        }
        charsTabBtn = Clone("CharsTabBtn", "Characters", 285f, 180f, () => SetTab(false));
        musicTabBtn = Clone("MusicTabBtn", "Music", 475f, 120f, () => SetTab(true));

        columnHeaders = new[] { window.Find("Col0"), window.Find("Col1"), window.Find("Col2") }.Select(t => t != null ? t.GetComponent<Text>() : null).ToArray();
        columnTexts = columnHeaders.Select(t => t != null ? t.text : null).ToArray();
        StyleTabs();
    }

    //Red X instead of the word "Close"; narrower, with the Reload button moved over to fill the gap
    private void StyleCloseButton()
    {
        const float width = 100f;
        var rt = (RectTransform)CloseBtn.transform;
        var narrowed = rt.sizeDelta.x - width;
        rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
        var reload = (RectTransform)ReloadBtn.transform;
        reload.anchoredPosition = new Vector2(reload.anchoredPosition.x + narrowed, reload.anchoredPosition.y);

        CloseBtn.GetComponent<Image>().color = new Color(0.85f, 0.12f, 0.12f, 1f);
        var label = CloseBtn.GetComponentInChildren<Text>();
        label.text = "X";
        label.color = Color.white;
        label.fontSize += 4;
        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
    }

    private void StyleTabs()
    {
        void Style(Button b, bool selected)
        {
            var image = b.GetComponent<Image>();
            image.color = selected ? tabColor : new Color(tabColor.r * 0.55f, tabColor.g * 0.55f, tabColor.b * 0.55f, tabColor.a);
        }
        Style(charsTabBtn, !musicTab);
        Style(musicTabBtn, musicTab);
    }

    private void SetTab(bool toMusic)
    {
        if(musicTab == toMusic) { return; }
        musicTab = toMusic;
        StyleTabs();
        SortBtn.gameObject.SetActive(!musicTab);
        for(var i = 0; i < columnHeaders.Length; i++)
        {
            if(columnHeaders[i] == null) { continue; }
            columnHeaders[i].gameObject.SetActive(!musicTab || i == 0);
            columnHeaders[i].text = musicTab && i == 0 ? "Folder / Track" : columnTexts[i];
        }

        if(musicTab)
        {
            ClearItems();
            if(catalog == null) { StatusText.text = "Loading the mod library..."; return; }
            music.Show(catalog, SearchInput.text);
        }
        else
        {
            music.Hide();
            StatusText.text = Hint;
            if(catalog != null) { Rebuild(); }
        }
    }

    public void Open()
    {
        cacheDir = System.IO.Path.Combine(Application.persistentDataPath, "cache");
        //Rows from the previous time the window was open (either tab) must not survive into this one
        music?.Hide();
        ClearItems();
        music = new MusicTabView(this, Content, RowTemplate, text => StatusText.text = text, cacheDir);
        if(musicTab) { SetTab(false); }
        service = new LibraryService(cacheDir);
        sortMode = (SortMode)Mathf.Clamp(GameDataManager.GM.PlyrConfigData.LibrarySort, 0, 2);
        UpdateSortLabel();
        Root.SetActive(true);
        StatusText.text = Hint;

        if(!GameDataManager.GM.PlyrConfigData.AgeNoticeAccepted)
        {
            Dialog.Show("18+ content",
                "All files and downloads in this library are sexually oriented adult material intended for adults 18 years of age or older.\n\n" +
                "If you are not of legal age, or are in a country where sexually explicit content is prohibited by law, do not continue.\n\n" +
                "The content consists of fictional works of art.",
                "I'm 18+", () =>
                {
                    GameDataManager.GM.PlyrConfigData.AgeNoticeAccepted = true;
                    GameDataManager.GM.SavePlyrCfgData();
                    Load(false);
                },
                "Cancel", Close);
            return;
        }
        Load(false);
    }

    public void Close()
    {
        loadCts?.Cancel();
        music?.Cancel();
        Root.SetActive(false);
    }

    private async void Load(bool force)
    {
        loadCts?.Cancel();
        loadCts = new CancellationTokenSource();
        var ct = loadCts.Token;

        ClearItems();
        music.Hide();
        music.Reset();
        StatusText.text = force ? "Reloading the mod library..." : "Loading the mod library...";
        try
        {
            var loaded = await service.LoadAsync(force, ct);
            if(ct.IsCancellationRequested) { return; }

            if(loaded.Error != null) { StatusText.text = loaded.Error + "\nPress Reload to try again."; return; }
            StatusText.text = loaded.Warning ?? Hint;
            Populate(loaded);
            if(musicTab) { music.Show(catalog, SearchInput.text); }
        }
        catch(OperationCanceledException) { }
    }

    private void ClearItems()
    {
        foreach(var item in items) { item.Object.SetActive(false); Destroy(item.Object); }
        items.Clear();
        iconQueue.Clear();
        Content.sizeDelta = new Vector2(Content.sizeDelta.x, 0);
    }

    public void Populate(LibraryCatalog newCatalog)
    {
        catalog = newCatalog;
        if(musicTab) { return; }
        Rebuild();
        if(catalog.Entries.Count == 0) { StatusText.text = "No downloadable series were found in the library."; }
    }

    private void Rebuild()
    {
        ClearItems();
        var groupIndex = 0;
        foreach(var group in LibraryArranger.Arrange(catalog.Entries, sortMode))
        {
            if(!string.IsNullOrEmpty(group.Heading))
            {
                var header = Instantiate(HeaderTemplate, Content);
                header.gameObject.SetActive(true);
                header.text = group.Heading;
                items.Add(new Item { Object = header.gameObject, Group = groupIndex, IsHeader = true });
            }

            foreach(var entry in group.Entries)
            {
                var row = Instantiate(RowTemplate, Content);
                row.gameObject.SetActive(true);
                row.Bind(entry);
                var captured = entry;
                row.ActionButton.onClick.AddListener(() => OnAction(captured, row));
                row.SeriesLink.DoubleClicked += () => OpenInBrowser(captured);
                items.Add(new Item { Object = row.gameObject, Row = row, Group = groupIndex });
                if(!string.IsNullOrEmpty(entry.IconFile)) { iconQueue.Enqueue(row); }
            }
            groupIndex++;
        }
        ApplyFilter(SearchInput.text);
        RefreshRowStates();
    }

    private void CycleSort()
    {
        sortMode = (SortMode)(((int)sortMode + 1) % 3);
        GameDataManager.GM.PlyrConfigData.LibrarySort = (int)sortMode;
        GameDataManager.GM.SavePlyrCfgData();
        UpdateSortLabel();
        if(catalog != null) { Rebuild(); }
    }

    private void UpdateSortLabel()
    {
        SortLabel.text = sortMode == SortMode.Sheet ? "Sort: Default" : sortMode == SortMode.Newest ? "Sort: Newest" : "Sort: Oldest";
    }

    private void ApplyFilter(string text)
    {
        if(musicTab) { music?.ApplyQuery(text); return; }
        var y = 0f;
        foreach(var item in items)
        {
            bool visible;
            if(item.IsHeader) { visible = items.Any(o => !o.IsHeader && o.Group == item.Group && Matches(o, text)); }
            else { visible = Matches(item, text); }

            item.Object.SetActive(visible);
            if(!visible) { continue; }

            var rt = (RectTransform)item.Object.transform;
            rt.anchoredPosition = new Vector2(0, -y);
            y += item.IsHeader ? HeaderHeight : RowHeight;
        }
        Content.sizeDelta = new Vector2(Content.sizeDelta.x, y);
        Content.anchoredPosition = new Vector2(Content.anchoredPosition.x, 0);

        //The cloned scroll view remembers its old position; start at the top
        if(scrollRect == null) { scrollRect = Content.GetComponentInParent<ScrollRect>(); }
        if(scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private static bool Matches(Item item, string text)
    {
        return ModFilter.Matches(item.Row.Entry.Series + " " + item.Row.Entry.Characters, text);
    }

    private void Update()
    {
        if(!IsOpen) { return; }
        for(var n = 0; n < 2 && iconQueue.Count > 0; n++)
        {
            var row = iconQueue.Dequeue();
            if(row != null && row.Entry != null) { row.SetIcon(LibraryIcons.Load(row.Entry.IconFile)); }
        }
        stateTimer += Time.unscaledDeltaTime;
        if(stateTimer < StateRefreshInterval) { return; }
        stateTimer = 0;
        if(musicTab) { music.RefreshStates(); } else { RefreshRowStates(); }
    }

    //Keys are the series URL, or series URL + "#" + quick key for one pack of a folder
    private static bool KeyMatches(string key, string url) { return key == url || key.StartsWith(url + "#"); }

    private void RefreshRowStates()
    {
        var snap = DownloadManager.Snapshot;
        foreach(var item in items)
        {
            if(item.IsHeader || item.Row.Entry.Kind == LinkKind.Browser) { continue; }
            var url = item.Row.Entry.Url;

            if(snap.Active && KeyMatches(snap.Key, url)) { item.Row.SetState(RowState.Active, snap.Fraction); }
            else if(snap.QueuedKeys.Any(k => KeyMatches(k, url))) { item.Row.SetState(RowState.Queued, 0f); }
            else { item.Row.SetState(RowState.Ready, 0f); }
        }
    }

    private void OpenInBrowser(LibraryEntry entry)
    {
        Application.OpenURL(entry.Url);
        StatusText.text = "Opened " + entry.Series + " in your browser.";
    }

    private void OnAction(LibraryEntry entry, LibraryRow row)
    {
        if(entry.Kind == LinkKind.Browser) { Application.OpenURL(entry.Url); return; }

        var snap = DownloadManager.Snapshot;
        if(snap.Active && KeyMatches(snap.Key, entry.Url)) { StatusText.text = entry.Series + " is downloading. Use the bar under the title to cancel."; return; }

        //A queued series is removed from the queue when its button is pressed again
        var queued = snap.QueuedKeys.Where(k => KeyMatches(k, entry.Url)).ToList();
        if(queued.Count > 0)
        {
            foreach(var key in queued) { DownloadManager.RemoveQueued(key); }
            StatusText.text = entry.Series + " removed from the queue.";
            RefreshRowStates();
            return;
        }

        if(entry.Kind == LinkKind.File) { Enqueue(entry, entry.Url, entry.Series, entry.Url, 0); }
        else { ChooseFromFolder(entry); }
    }

    private async void ChooseFromFolder(LibraryEntry entry)
    {
        StatusText.text = "Loading packs for " + entry.Series + "...";
        var listing = await new PackDownloader().ListFolderAsync(entry.Url, CancellationToken.None);
        if(!IsOpen) { return; }
        StatusText.text = string.Empty;

        if(listing.Error != null) { Dialog.Show("Could not list packs", listing.Error, "OK", null); return; }
        if(listing.Files.Count == 0) { Dialog.Show("No packs", "This folder has no downloadable files.", "OK", null); return; }

        var files = listing.Files;
        if(files.Count == 1) { EnqueuePacks(entry, new List<FolderFile> { files[0] }); return; }

        //Several packs can be ticked; they download one after another. Nothing is ticked by default (packs can be large).
        Dialog.ShowChecklist("Choose packs", TextFit.MiddleEllipsis(entry.Series, 60) + " has several packs.\nTick the ones you want; they download one after another.",
            files.Select(f => TextFit.PackLabel(f.Name) + "  (" + TransferFormat.Size(f.SizeBytes) + ")").ToList(),
            "Download selected", chosen => EnqueuePacks(entry, chosen.Select(i => files[i]).ToList()),
            "Cancel", null, false,
            selected => "Download " + selected.Count + " (" + TransferFormat.Size(selected.Sum(i => files[i].SizeBytes)) + ")");
    }

    private void EnqueuePacks(LibraryEntry entry, IReadOnlyList<FolderFile> packs)
    {
        var added = 0;
        foreach(var pack in packs)
        {
            var key = entry.Url + "#" + pack.QuickKey;
            if(DownloadManager.Enqueue(MakeRequest(entry, key, entry.Series + " - " + TextFit.PackLabel(pack.Name), MediaFirePage.FileUrlFromQuickKey(pack.QuickKey), pack.SizeBytes))) { added++; }
        }
        StatusText.text = added == packs.Count
            ? added + (added == 1 ? " pack" : " packs") + " added to downloads. You can close this window."
            : added + " of " + packs.Count + " packs added; the rest were already downloading or queued.";
        RefreshRowStates();
    }

    private DownloadRequest MakeRequest(LibraryEntry entry, string key, string title, string pageUrl, long expectedBytes)
    {
        return new DownloadRequest
        {
            Key = key, Title = title, PageUrl = pageUrl, ExpectedBytes = expectedBytes,
            NamesKnown = CharacterInfo.NamesKnown(entry.Characters),
            ModsDir = GameDataManager.GM.GetModsPath(), WorkDir = cacheDir
        };
    }

    private void Enqueue(LibraryEntry entry, string key, string title, string pageUrl, long expectedBytes)
    {
        var added = DownloadManager.Enqueue(MakeRequest(entry, key, title, pageUrl, expectedBytes));
        StatusText.text = added ? title + " added to downloads. You can close this window." : title + " is already downloading or queued.";
        RefreshRowStates();
    }

    private void OnDestroy()
    {
        loadCts?.Cancel();
    }
}
