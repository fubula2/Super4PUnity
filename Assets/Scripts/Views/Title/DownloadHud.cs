using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

//Title-screen view of DownloadManager: progress strip, toasts, and the questions the worker asks (import choice, overwrite).
//Also shows the app version and the update notice.
public class DownloadHud : MonoBehaviour
{
    public GameObject Strip;
    public Image StripFill;
    public Text StripLabel;
    public Button StripButton;
    public Button StripCancel;

    public GameObject Toast;
    public Text ToastText;
    public Image ToastBackground;
    public Button ToastRefresh;
    public Button ToastShowFolder;
    public Button ToastClose;

    public Button VersionButton;
    public Text VersionText;

    public Dialog Dialog;
    public GetModsPanel GetModsPanel;

    private static readonly Color CancelRed = new Color(0.85f, 0.12f, 0.12f, 1f);
    private const float ToastSeconds = 9f;
    private const float ErrorToastSeconds = 14f;
    private const float UpdateToastSeconds = 25f;
    private static readonly TimeSpan UpdateCheckInterval = TimeSpan.FromHours(24);

    private static bool cleaned;
    private Color versionNormalColor;
    private bool versionColorSet;
    private static bool updateChecked;
    private readonly Queue<DownloadNotification> toasts = new Queue<DownloadNotification>();
    private DownloadNotification shownToast;
    private float toastTimer;
    private PendingPrompt shownPrompt;
    private bool promptDialogOpen;
    private Color toastColor;
    private string refreshLabel;
    private string folderLabel;

    private void Awake()
    {
        toastColor = ToastBackground.color;
        refreshLabel = ToastRefresh.GetComponentInChildren<Text>().text;
        folderLabel = ToastShowFolder.GetComponentInChildren<Text>().text;
        Strip.SetActive(false);
        Toast.SetActive(false);
        LayoutToastButtons();

        //Long series/pack names shrink to fit the bar instead of running into the Cancel button
        StripLabel.resizeTextForBestFit = true; StripLabel.resizeTextMinSize = 9; StripLabel.resizeTextMaxSize = 14;
        StripLabel.horizontalOverflow = HorizontalWrapMode.Wrap; StripLabel.verticalOverflow = VerticalWrapMode.Truncate;

        //Linux support is a beta, so the version label says so
        //A little further from the bar's corner rivet
        var versionRect = (RectTransform)VersionButton.transform;
        versionRect.anchoredPosition += new Vector2(20f, 0f);
        versionNormalColor = VersionText.color;

        VersionText.text = "v" + Application.version + (Application.platform == RuntimePlatform.LinuxPlayer ? " beta" : string.Empty);
        VersionButton.onClick.AddListener(() =>
        {
            ShowToast(new DownloadNotification { Text = "Checking for updates..." });
            UpdateChecker.Check(Application.version, true);
        });

        StripButton.onClick.AddListener(() => { if(!GetModsPanel.IsOpen) { GetModsPanel.Open(); } });
        StripCancel.onClick.AddListener(DownloadManager.CancelCurrent);
        //MarioFont's line height is taller than this 26 px button, so the label must be allowed to overflow or it is clipped away
        var cancelLabel = StripCancel.GetComponentInChildren<Text>();
        cancelLabel.text = "X";
        cancelLabel.color = Color.white;
        cancelLabel.fontSize = 18;
        cancelLabel.alignment = TextAnchor.MiddleCenter;
        cancelLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        cancelLabel.verticalOverflow = VerticalWrapMode.Overflow;
        StripCancel.GetComponent<Image>().color = new Color(0.85f, 0.12f, 0.12f, 1f);
        ToastClose.onClick.AddListener(DismissToast);
        ToastRefresh.onClick.AddListener(OnToastPrimary);
        ToastShowFolder.onClick.AddListener(OnToastSecondary);

        //Leftovers from an interrupted run (nothing is deleted while a download is active)
        if(!cleaned)
        {
            cleaned = true;
            DownloadManager.CleanupStaleWork(Path.Combine(Application.persistentDataPath, "cache"));
        }
    }

    private void Update()
    {
        UpdateStrip();
        UpdatePrompt();
        UpdateUpdateCheck();
        UpdateToasts();
    }

    private void OnToastPrimary()
    {
        if(shownToast != null && shownToast.IsUpdate)
        {
            Application.OpenURL(shownToast.Url);
            DismissToast();
            return;
        }
        GameDataManager.GM.RefreshMods();
        MainController.MC.GoToScene("Title");
    }

    private void OnToastSecondary()
    {
        if(shownToast == null) { return; }
        if(shownToast.IsUpdate)
        {
            GameDataManager.GM.PlyrConfigData.SkippedUpdateVersion = shownToast.Version;
            GameDataManager.GM.SavePlyrCfgData();
            ShowToast(new DownloadNotification { Text = "OK, you won't be reminded about v" + shownToast.Version + "." });
            return;
        }
        if(!string.IsNullOrEmpty(shownToast.ModsDir)) { Application.OpenURL(new Uri(shownToast.ModsDir).AbsoluteUri); }
    }

    //Close button is red; the two action buttons sit right next to it, and the message ends before them
    private void LayoutToastButtons()
    {
        const float gap = 10f;
        ToastClose.GetComponent<Image>().color = CancelRed;
        ToastClose.GetComponentInChildren<Text>().color = Color.white;

        var close = (RectTransform)ToastClose.transform;
        var folder = (RectTransform)ToastShowFolder.transform;
        var refresh = (RectTransform)ToastRefresh.transform;
        var closeLeft = -close.anchoredPosition.x + close.sizeDelta.x + gap;   //distance of the close button's left edge from the right side, plus a gap
        folder.anchoredPosition = new Vector2(-closeLeft + 0f, folder.anchoredPosition.y);
        refresh.anchoredPosition = new Vector2(-(closeLeft + folder.sizeDelta.x + gap), refresh.anchoredPosition.y);

        var text = ToastText.rectTransform;
        var toastWidth = ((RectTransform)Toast.transform).sizeDelta.x;
        var refreshLeft = toastWidth - (closeLeft + folder.sizeDelta.x + gap + refresh.sizeDelta.x);
        text.sizeDelta = new Vector2(refreshLeft - gap - text.anchoredPosition.x, text.sizeDelta.y);
    }

    private void UpdateStrip()
    {
        var snap = DownloadManager.Snapshot;
        Strip.SetActive(snap.Active);
        if(!snap.Active) { return; }

        StripFill.fillAmount = snap.Fraction;
        var queued = snap.QueuedKeys.Count > 0 ? "   (+" + snap.QueuedKeys.Count + " queued)" : string.Empty;
        if(snap.WaitingForPrompt) { StripLabel.text = snap.Title + "   waiting for your choice" + queued; }
        else if(snap.Stage == "Extracting") { StripLabel.text = snap.Title + "   extracting..." + queued; }
        else
        {
            StripLabel.text = snap.Title + "   " + Mathf.RoundToInt(snap.Fraction / 0.8f * 100f).ToString() + "%   " +
                TransferFormat.Speed(snap.BytesPerSecond) + "   " + TransferFormat.TimeLeft(snap.SecondsLeft) + " left" + queued;
        }
    }

    private void UpdatePrompt()
    {
        var p = DownloadManager.PendingPrompt;
        if(p == null)
        {
            if(shownPrompt != null && promptDialogOpen) { Dialog.Hide(); }
            shownPrompt = null;
            promptDialogOpen = false;
            return;
        }
        if(p == shownPrompt || Dialog.IsOpen) { return; }

        shownPrompt = p;
        promptDialogOpen = true;
        if(p.Kind == PromptKind.Select)
        {
            var body = p.IsImport ? "Found these mods. Untick any you do not want." + (string.IsNullOrEmpty(p.Note) ? "" : "\n" + p.Note)
                : TextFit.MiddleEllipsis(p.Title, 78) + "\nUntick the characters you do not want to import.";
            Dialog.ShowChecklist(p.IsImport ? "Confirm import" : "Choose characters", body,
                p.Names.Select(CharDisplayName.From).ToList(), p.IsImport ? "Confirm import" : "Import selected",
                chosen => { promptDialogOpen = false; p.CompleteSelect(chosen.Select(i => p.Names[i]).ToList()); },
                "Cancel import", () => { promptDialogOpen = false; p.CompleteSelect(null); }, cancelIsRed: true);
        }
        else
        {
            if(p.IsMusic)
            {
                Dialog.Show("Already in your music folder", p.Names[0] + "\n\nOverwrite it with the downloaded version?",
                    "Overwrite", () => { promptDialogOpen = false; p.CompleteConflict(ConflictChoice.Overwrite); },
                    "Skip", () => { promptDialogOpen = false; p.CompleteConflict(ConflictChoice.Skip); });
                return;
            }
            Dialog.Show("Already installed", string.Join("\n", p.Names.Take(10).Select(CharDisplayName.From)) + (p.Names.Count > 10 ? "\n..." : "") +
                "\n\nOverwrite these with the downloaded version?",
                "Overwrite", () => { promptDialogOpen = false; p.CompleteConflict(ConflictChoice.Overwrite); },
                "Skip", () => { promptDialogOpen = false; p.CompleteConflict(ConflictChoice.Skip); });
        }
    }

    //Checks for a newer release once a day (silently) and whenever the version label is clicked
    private void UpdateUpdateCheck()
    {
        if(!GameDataManager.IsPlayerDataLoaded) { return; }
        var config = GameDataManager.GM.PlyrConfigData;

        if(!updateChecked)
        {
            updateChecked = true;
            if(UpdateThrottle.ShouldCheck(config.LastUpdateCheckUtc, DateTime.UtcNow, UpdateCheckInterval)) { UpdateChecker.Check(Application.version, false); }
        }

        //Once the saved settings are there, show whether this version is out of date (known from the last check)
        if(!versionColorSet) { versionColorSet = true; ColorVersionLabel(config); }

        while(UpdateChecker.TryTakeResult(out var result))
        {
            //A failed check is silent unless the player asked, and does not count as a check (it is retried next launch)
            if(result.Status == UpdateStatus.Failed)
            {
                if(result.Manual) { ShowToast(new DownloadNotification { Text = "Could not check for updates: " + result.Error, IsError = true }); }
                continue;
            }

            config.LastUpdateCheckUtc = UpdateThrottle.Stamp(DateTime.UtcNow);
            if(result.Release != null) { config.NewestKnownVersion = result.Release.Version; ColorVersionLabel(config); }
            GameDataManager.GM.SavePlyrCfgData();

            if(result.Status == UpdateStatus.Available)
            {
                if(result.Manual || !UpdateThrottle.IsSkipped(result.Release.Version, config.SkippedUpdateVersion)) { ShowToast(UpdateNotice(result.Release)); }
            }
            else if(result.Manual)
            {
                var detail = result.Status == UpdateStatus.NoReleases ? "no newer release has been published" : "this is the latest version";
                ShowToast(new DownloadNotification { Text = "You're up to date (v" + Application.version + "): " + detail + "." });
            }
        }
    }

    //Red while a newer version has been released; the normal colour otherwise
    private void ColorVersionLabel(PlayerConfigData config)
    {
        VersionText.color = AppVersion.IsNewer(config.NewestKnownVersion, Application.version) ? CancelRed : versionNormalColor;
    }

    private static DownloadNotification UpdateNotice(ReleaseInfo release)
    {
        var text = "Update available: v" + release.Version + " (you have v" + Application.version + ")";
        var firstLine = (release.Notes ?? string.Empty).Split('\n').FirstOrDefault(l => !string.IsNullOrWhiteSpace(l));
        if(!string.IsNullOrEmpty(firstLine)) { text += "\n" + (firstLine.Length > 90 ? firstLine.Substring(0, 90) + "..." : firstLine); }
        return new DownloadNotification { Text = text, IsUpdate = true, Url = release.Url, Version = release.Version };
    }

    private void UpdateToasts()
    {
        //Notifications can only be handled once the game data is loaded; background installs add to the list on the main thread
        if(GameDataManager.IsPlayerDataLoaded)
        {
            while(DownloadManager.TryTakeNotification(out var n))
            {
                if(n.CanRefresh) { GameDataManager.GM.ReloadCharDirsOnly(); }
                toasts.Enqueue(n);
            }
        }

        if(shownToast == null)
        {
            if(toasts.Count > 0) { ShowToast(toasts.Dequeue()); }
            return;
        }

        toastTimer -= Time.unscaledDeltaTime;
        if(toastTimer <= 0f) { DismissToast(); }
    }

    //Replaces whatever toast is showing
    private void ShowToast(DownloadNotification n)
    {
        shownToast = n;
        toastTimer = n.IsUpdate ? UpdateToastSeconds : n.IsError ? ErrorToastSeconds : ToastSeconds;
        ToastText.text = n.Text;
        ToastBackground.color = n.IsError ? new Color(0.45f, 0.08f, 0.08f, 1f) : toastColor;

        var showButtons = n.CanRefresh || n.IsUpdate;
        ToastRefresh.GetComponentInChildren<Text>().text = n.IsUpdate ? "Download" : refreshLabel;
        ToastShowFolder.GetComponentInChildren<Text>().text = n.IsUpdate ? "Skip version" : folderLabel;
        ToastRefresh.gameObject.SetActive(showButtons);
        ToastShowFolder.gameObject.SetActive(showButtons);
        Toast.SetActive(true);
    }

    private void DismissToast()
    {
        shownToast = null;
        Toast.SetActive(false);
    }
}
