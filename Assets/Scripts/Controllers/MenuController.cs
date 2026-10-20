using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.InputSystem.InputAction;

//Parent controller for title screen (mod loading/config). Passes on inputs to relevant game objects
public class MenuController : InputController
{
    public SourcePanel SourcePanel;
    public ModPanel ModPanel;
    public MusicManagerPanel MusicPanel;
    public InputField PresetInputField;
    public HelpPanel HelpPanel;

    public Dropdown ResDropdown;
    public Toggle FullscreenToggle;
    public Dropdown DefaultCharStyleDD;
    public Dropdown DefaultPoseStyleDD;
    public Button ModsFolderBtn;
    public Button RefreshBtn;
    public Button GetModsBtn;
    public GetModsPanel GetModsPanel;

    private IFolderPicker FolderPicker;
    private Dropdown FpsDropdown;
    private const float FpsBoxWidth = 130f;

    private readonly string SPREADSHEET_URL = "https://docs.google.com/spreadsheets/d/1kAkBLDDOEF-v0_LPNFYi4IwR0NpWBF575WicjG6tEIw/";
    //private readonly string SPREADSHEET_URL = "https://docs.google.com/document/d/1FP3rpCCxqR_ar4e6-vXTJwo41zf72_0a5tq3VNmpg9Q/edit?usp=sharing";
    public bool IsUpdateDelegated = false;

    public static MenuController MenuCtrl;
    
    protected virtual void Awake()
    {
        //Singleton pattern
        if (MenuCtrl == null) {
            MenuCtrl = this;
        }
        else if (MenuCtrl != this) {
            Destroy(MenuCtrl);
            MenuCtrl = this;
        }     
    }

    override protected void Start() 
    {
        base.Start();
        var holder = CanvasContentCentering.Apply(GetComponentInChildren<Canvas>(true));
        if(holder != null) { StretchTitleBar(); }
        BuildFpsRow();
        if(MobileLayout.Active) { ApplyMobileMenu(); }

        HelpPanel.Initialize("SMW-MessageBlock");

        #if UNITY_STANDALONE || UNITY_EDITOR
            FolderPicker = gameObject.AddComponent<DesktopFolderPicker>();
            ModsFolderBtn.onClick.AddListener(OnChooseModsFolder);
        #elif UNITY_ANDROID
            ModsFolderBtn.onClick.AddListener(OnShowModsFolder);
        #else
            ModsFolderBtn.gameObject.SetActive(false);
        #endif
        RefreshBtn.onClick.AddListener(OnRefreshMods);
        GetModsBtn.onClick.AddListener(GetModsPanel.Open);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        if(System.Environment.GetEnvironmentVariable("S4P_TEST_HIDE_MODSBTN") == "1") { ModsFolderBtn.gameObject.SetActive(false); }
#endif
        WidenTopButtons();
        PackTopButtons();
        StyleTopButtons();

        if(GameDataManager.IsPlayerDataLoaded)
        {
            Initialize();
        }
        else
        {
            MainController.MC.UpdateDelegate += Initialize;
            IsUpdateDelegated = true;
        }
    }
    
    public void Initialize() 
    {
        if(IsUpdateDelegated)
        {
            MainController.MC.UpdateDelegate -= Initialize;
            IsUpdateDelegated = false;
        }

        MusicPanel.Initialize();
        ModPanel.Initialize();
        SourcePanel.Initialize();

        ResDropdown.SetValueWithoutNotify((int)GameDataManager.GM.PlyrConfigData.AspectResolution);
        FullscreenToggle.SetIsOnWithoutNotify(GameDataManager.GM.PlyrConfigData.IsFullscreen);
        if(FpsDropdown != null) { FpsDropdown.SetValueWithoutNotify(FrameRatePolicy.IndexOf(MobilePerformance.CurrentFps)); FpsDropdown.RefreshShownValue(); }
        DefaultCharStyleDD.SetValueWithoutNotify((int)GameDataManager.GM.PlyrConfigData.CharPlaybackStyle);
        DefaultPoseStyleDD.SetValueWithoutNotify((int)GameDataManager.GM.PlyrConfigData.PosePlaybackStyle);

        MainController.MC.GS_Current = GameState.Active;
        if(Application.platform == RuntimePlatform.Android) { StartCoroutine(DefaultMusicCopier.CopyOnce(OnRefreshMods)); }
    }

    //Get Mods and Refresh are a little wider so they are easier to hit and notice; PackTopButtons then lines all three up
    private void WidenTopButtons()
    {
        void Widen(Button b, float width) { var rt = (RectTransform)b.transform; rt.sizeDelta = new Vector2(width, rt.sizeDelta.y); }
        Widen(GetModsBtn, 220f);
        Widen(RefreshBtn, 200f);
    }

    //The three top buttons look like the Save button (its green picture and white text), so they stand out from the yellow bar
    private void StyleTopButtons()
    {
        var save = FindDeep(GetComponentInChildren<Canvas>(true).transform, "SavePresetBtn");
        var source = save != null ? save.GetComponent<Image>() : null;
        if(source == null) { return; }
        var sourceLabel = save.GetComponentInChildren<Text>();
        var sourceButton = save.GetComponent<Button>();   //the green is the button's colour tint, not the picture's own colour

        foreach(var button in new[] { GetModsBtn, ModsFolderBtn, RefreshBtn })
        {
            var image = button.GetComponent<Image>();
            image.sprite = source.sprite;
            image.type = source.type;
            image.color = source.color;
            image.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
            if(sourceButton != null) { button.transition = sourceButton.transition; button.colors = sourceButton.colors; }
            var label = button.GetComponentInChildren<Text>();
            if(label != null && sourceLabel != null) { label.color = sourceLabel.color; }
        }
    }

    //The top-right buttons sit side by side from the right edge; a hidden one must not leave a gap
    private void PackTopButtons()
    {
        const float gap = 10f;
        var x = 0f;
        var first = true;
        foreach(var button in new[] { RefreshBtn, ModsFolderBtn, GetModsBtn })
        {
            if(!button.gameObject.activeSelf) { continue; }
            var rt = (RectTransform)button.transform;
            if(first) { x = rt.anchoredPosition.x; first = false; }
            else { rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y); }
            x -= rt.sizeDelta.x + gap;
        }
    }

    //On a wide screen the title bar and its buttons must reach the real screen edges, not just the edges of the centred content
    private void StretchTitleBar()
    {
        var canvasRoot = GetComponentInChildren<Canvas>(true).transform;
        var panel = FindDeep(canvasRoot, "TitlePanel") as RectTransform;
        if(panel == null) { return; }

        var height = panel.sizeDelta.y;
        panel.SetParent(canvasRoot, false);
        panel.SetAsFirstSibling();
        panel.anchorMin = new Vector2(0, 1); panel.anchorMax = new Vector2(1, 1); panel.pivot = new Vector2(0.5f, 1);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(0, height);
    }

    //Android: tell the player where the mods folder is and offer to open it in the Files app
    private void OnShowModsFolder()
    {
        var path = GameDataManager.GM.GetModsPath();
        var where = AndroidFolderLink.Friendly(path);
        GetModsPanel.Dialog.Show("Mods folder", "Put your mod folders (CHAR_...) here, then press Refresh:\n\n" + where,
            "Open in Files", () =>
            {
                if(!AndroidFiles.OpenFolder(path)) { GetModsPanel.Dialog.Show("Mods folder", "Open the Files app and go to:\n\n" + where, "OK", null); }
            },
            "Close", null);
    }

    //Display Settings gets a frame-rate choice (30 or 60) under the existing rows, built from copies of the Resolution row
    private void BuildFpsRow()
    {
        var root = GetComponentInChildren<Canvas>(true).transform;
        var resLabel = FindDeep(root, "ResLabel") as RectTransform;
        var fullscreenLabel = FindDeep(root, "FullscreenLabel") as RectTransform;
        if(resLabel == null || fullscreenLabel == null) { return; }

        //Phones hide the Resolution and Fullscreen rows, so FPS takes the first row; elsewhere it goes below Fullscreen
        var rowHeight = fullscreenLabel.anchoredPosition.y - resLabel.anchoredPosition.y;
        var offset = MobileLayout.Active ? 0f : rowHeight * 2f;

        var label = Instantiate(resLabel, resLabel.parent);
        label.name = "FpsLabel";
        label.anchoredPosition += new Vector2(0, offset);
        label.GetComponent<Text>().text = "FPS:";

        //The Resolution dropdown is a child of its label, so the copied label already carries its own copy of the dropdown
        FpsDropdown = label.GetComponentInChildren<Dropdown>(true);
        FpsDropdown.name = "FpsDropdown";
        FpsDropdown.onValueChanged = new Dropdown.DropdownEvent();   //the copy would otherwise keep the resolution dropdown's saved handler
        //"30 FPS" needs far less room than a resolution, so the box is narrower; its left edge stays lined up with the others
        var box = (RectTransform)FpsDropdown.transform;
        var left = box.anchoredPosition.x - box.pivot.x * box.sizeDelta.x;
        box.sizeDelta = new Vector2(FpsBoxWidth, box.sizeDelta.y);
        box.anchoredPosition = new Vector2(left + box.pivot.x * FpsBoxWidth, box.anchoredPosition.y);
        FpsDropdown.ClearOptions();
        FpsDropdown.AddOptions(new System.Collections.Generic.List<string> { "30", "60" });
        FpsDropdown.onValueChanged.AddListener(OnFpsChanged);   //its value is set in Initialize, once the saved settings are loaded
    }

    private void OnFpsChanged(int index)
    {
        GameDataManager.GM.PlyrConfigData.TargetFps = FrameRatePolicy.FromIndex(index);
        GameDataManager.GM.SavePlyrCfgData();
        MobilePerformance.ApplyFrameRate();
    }

    //Phone layout: the desktop window options (resolution, fullscreen) do not apply
    private void ApplyMobileMenu()
    {
        var root = GetComponentInChildren<Canvas>(true).transform;
        foreach(var name in new[] { "ResLabel", "FullscreenLabel" })
        {
            var t = FindDeep(root, name);
            if(t != null) { t.gameObject.SetActive(false); }
        }
        ResDropdown.gameObject.SetActive(false);
        FullscreenToggle.gameObject.SetActive(false);
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        foreach(Transform child in parent)
        {
            if(child.name == name) { return child; }
            var found = FindDeep(child, name);
            if(found != null) { return found; }
        }
        return null;
    }

    public void OnChooseModsFolder()
    {
        if(FolderPicker == null || GameDataManager.GM == null) { return; }

        FolderPicker.Pick("Choose mods folder", GameDataManager.GM.GetModsPath(), path =>
        {
            GameDataManager.GM.SetModsPath(path);
            //Reload the title scene so mod, music and preset lists rebuild from the new folder
            MainController.MC.GoToScene("Title");
        });
    }

    public void OnRefreshMods()
    {
        if(GameDataManager.GM == null) { return; }

        GameDataManager.GM.RefreshMods();
        MainController.MC.GoToScene("Title");
    }

    public void OnLibraryLinkClick()
    {
        Application.OpenURL(SPREADSHEET_URL);
    }

    public void OnSetCharStyle(int value)
    {
        GameDataManager.GM.PlyrConfigData.CharPlaybackStyle = (PlaybackStyle)value;
    }

    public void OnSetPoseStyle(int value)
    {
        GameDataManager.GM.PlyrConfigData.PosePlaybackStyle = (PlaybackStyle)value;
    }

    public void OnPlay()
    {
        GameDataManager.GM.SetSelectedMods(SourcePanel.GetSelectedMods());
        GameDataManager.GM.SetSelectedMusic(SourcePanel.GetSelectedMusic());
        ModPanel.RemoveUnusedSprites();
        if(LinuxVideoPrep.Needed)
        {
            LinuxPlayFlow.Run(GetModsPanel.Dialog, GameDataManager.GM.SelectedMods, () => MainController.MC.GoToScene("Game"));
            return;
        }
        MainController.MC.GoToScene("Game");
    }

    public void OnHelpClick()
    {
        HelpPanel.OnToggleHelp();
    }

    public void SwitchResolution(int res)
    {
        MainController.MC.SwitchResolution(res);
    }

    //Typing in a text box must not trigger single-key shortcuts (help, fullscreen, escape)
    private bool IsTyping => PresetInputField.isFocused || ModPanel.SearchInput.isFocused || GetModsPanel.IsTyping;

    //--Player Input Events--
    override public void OnAnyKey(CallbackContext context)
    {
        if(!context.performed || !HelpPanel.IsHelpOpen || !HelpPanel.IsHelpTransitionComplete) { return; }
        HelpPanel.OnToggleHelp();
    } 

    override public void OnToggleHelp(CallbackContext context)
    {
        if(!context.performed || IsTyping || HelpPanel.IsHelpOpen || !HelpPanel.IsHelpTransitionComplete) { return; }
        OnHelpClick();
    } 

    override public void OnFullscreen(CallbackContext context)
    {
        if(!context.performed || IsTyping || HelpPanel.IsHelpOpen || !HelpPanel.IsHelpTransitionComplete) { return; }
        FullscreenToggle.isOn = !FullscreenToggle.isOn;
    } 

    override public void OnEscape(CallbackContext context)
    {
        if(!context.performed || IsTyping) { return; }
        if(GetModsPanel.Dialog.IsOpen) { return; }
        if(GetModsPanel.IsOpen) { GetModsPanel.Close(); return; }
        Application.Quit();
    }
}