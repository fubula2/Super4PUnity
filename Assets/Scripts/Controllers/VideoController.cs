using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.InputSystem.InputAction;

public class VideoController : InputController
{
    public Canvas VideoControlsUI;

    public MusicPanel MusicPanel;
    public CharacterPanel CharacterPanel;
    public PosePanel PosePanel;
    public HelpPanel HelpPanel;
    public VideoDeck VideoDeck;
    public ResolutionPicker ResolutionPicker;

    public bool IsLoaded;

    public int ActiveCharIndex;
    public int ActivePoseIndex;
    public int SelectedCharIndex;
    public int NextCharIndex;
    public int NextPoseIndex;

    private int CharCount;
    public int ActivePoseCount;
    public int NextPoseCount;
    private int SelectedPoseCount;

    public static VideoController VC;

    protected virtual void Awake()
    {
        //Singleton pattern
        if (VC == null) {
            VC = this;
        }
        else if (VC != this) {
            Destroy(VC);
            VC = this;
        }     
    }

    override protected void Start()
    {
        MainController.MC.SetState(GameState.Loading);
        base.Start();

        NextCharIndex = NextPoseIndex = -1;
        HelpPanel.Initialize("SMW-PrincessHelp");

        //Load Music and Audio
        MusicPanel.Initialize();
        StartCoroutine(MusicPanel.LoadMusic());

        //TODO: Move VideoDeck below these and set initial load to one video if only one in characters
        //Initialize Videoplayers, Pose Panel (empty), and Character Panel (selects & sets next)
        VideoDeck.Initialize(MainController.MC.MusicPlayer);
        VideoDeck.OnReady += OnLoadComplete;
        VideoDeck.OnCycleComplete += OnCycleComplete;
        VideoDeck.OnClipFailed += OnClipFailed;
        PosePanel.Initialize();
        CharCount = CharacterPanel.Initialize(); //Sets SelectedCharIndex


        CharacterPanel.SetNextCharacter(SelectedCharIndex, false);
        SetNextPoseIndex(0);
        PosePanel.OnSelectItem(NextPoseIndex, true); //Results in loading 1st video
    
        OnCycleComplete();
        SelectedCharIndex = -1;
    }

    public void OnLoadComplete()
    {
        if(!MusicPanel.IsLoaded || !VideoDeck.IsLoaded) { return; }

        MainController.MC.SetState(GameState.Active);
        MusicPanel.PlayMusic();
        VideoDeck.PlayVideos();
        IsLoaded = true;
    }

    //Current video just ended (or first cycle started)
    public void OnCycleComplete()
    {
        //Set ActiveCharIndex (ActivePoseCount) and ActivePoseIndex
        CharacterPanel.SetActivecharacter(NextCharIndex);
        ActivePoseIndex = NextPoseIndex;

        //If poses shown are for active character, update styling
        if(SelectedCharIndex == ActiveCharIndex || SelectedCharIndex == -1) {
            PosePanel.SetActivePose(ActivePoseIndex);
        }

        //Set NextCharacterIndex and NextPoseIndex
        OnToggleCharStyle();
    }

    public void OnToggleCharStyle()
    {
        var isCharChanged = false;

        if(PosePanel.PlaybackStyle == PlaybackStyle.Order)
        {
            if(ActivePoseIndex >= ActivePoseCount - 1) 
            {
                isCharChanged = CharacterPanel.SetNextCharacter(-1, false);
                //OnCharacterSelected(ActivePoseCount, false);
            }
            else {
                isCharChanged = CharacterPanel.SetNextCharacter(ActiveCharIndex, false);
            }
        }
        else {
            isCharChanged = CharacterPanel.SetNextCharacter(-1, false);
        }

        if(isCharChanged) {
            OnCharacterSelected(ActivePoseCount, false);
        }
        SetNextPoseIndex();
        // if(!CheckLooping()) {
            
        // }
        // SetNextPoseIndex(ActivePoseIndex >= NextPoseCount - 1 ? 0 : -1);
    }

    public void OnTogglePoseStyle()
    {
        //If Poses in order but not looping character, don't change character if not at end
        if(PosePanel.PlaybackStyle == PlaybackStyle.Order && CharacterPanel.PlaybackStyle != PlaybackStyle.Loop
            && ActivePoseIndex < ActivePoseCount - 1)
        {
            CharacterPanel.SetNextCharacter(ActiveCharIndex, false);
        }
        else if(PosePanel.PlaybackStyle != PlaybackStyle.Order && CharacterPanel.PlaybackStyle != PlaybackStyle.Loop)
        {
            CharacterPanel.SetNextCharacter(-1, false);
        }
        else
        {
            CharacterPanel.SetNextCharacter(NextCharIndex, false);
        }
        SetNextPoseIndex();
        PosePanel.StyleBtns((SelectedCharIndex == -1 || SelectedCharIndex == ActiveCharIndex), NextCharIndex == ActiveCharIndex);
        SelectedCharIndex = -1;
    }

    //Before this, NextCharIndex should be set!
    public void OnSetNextCharIndex()
    {
        SetNextPoseIndex();
    }

    public void SetNextPoseIndex(int index = -1)
    {
        if(index == -1)
        {
            index = ActivePoseIndex;
            switch(PosePanel.PlaybackStyle)
            {
                case(PlaybackStyle.Order):
                    if(index >= ActivePoseCount - 1) { index = 0; }
                    else { index++; }
                    break;

                case(PlaybackStyle.Random):
                    index = UnityEngine.Random.Range(0, NextPoseCount);
                    break;
            }
        }
        if(index >= NextPoseCount) { index = 0; }

        NextPoseIndex = index;
        
        VideoDeck.LoadNextVideo(CharacterPanel.GetVideoPath(NextPoseIndex));

        if(SelectedCharIndex == ActiveCharIndex 
            || (SelectedCharIndex == -1 && NextCharIndex == ActiveCharIndex)) 
        { 
            PosePanel.SetNextPose(NextPoseIndex); 
        }
    }

    //Before this, SelectedCharIndex should be set!
    public void OnCharacterSelected(int poseCount, bool isManual = true)
    {
        if(isManual)
        {
            SelectedPoseCount = poseCount;
            PosePanel.CreatePoses(poseCount);
            PosePanel.StyleBtns(SelectedCharIndex == ActiveCharIndex, SelectedCharIndex == NextCharIndex);
        }
        else
        {
            PosePanel.CreatePoses(poseCount);
        }
    }

    //Set our next character based on the one currently selected
    public void OnPoseSelected(int index)
    {
        NextPoseIndex = index;
        CharacterPanel.SetNextCharacter(SelectedCharIndex > -1 ? SelectedCharIndex : ActiveCharIndex, false);
        PosePanel.StyleBtns(NextCharIndex == ActiveCharIndex, false);
        CharacterPanel.ClearSelectedItem();
        SelectedCharIndex = -1;
        VideoDeck.LoadNextVideo(CharacterPanel.GetVideoPath(NextPoseIndex));
    }

    //A clip that cannot play is skipped by the deck; if it was the very first clip there is nothing to show
    private void OnClipFailed(string videoPath)
    {
        if(!VideoDeck.IsLoaded) { MainController.MC.GoToScene("Title"); }
    }

    public bool CheckLooping()
    {
        return CharacterPanel.PlaybackStyle == PlaybackStyle.Loop && PosePanel.PlaybackStyle == PlaybackStyle.Loop;
    }

    public void SetVideoVolume(float volume)
    {
        VideoDeck.SetVolume(volume);
    }

    public void OnHelpClick()
    {
        HelpPanel.OnToggleHelp();
    }

    //--Player Input Events--
    override public void OnSwitchMusicPrev(CallbackContext context)
    {
        if(!context.performed || !IsLoaded) { return; }
        MusicPanel.NextMusicTrack(-1);
    }

    override public void OnSwitchMusicNext(CallbackContext context)
    {
        if(!context.performed || !IsLoaded) { return; }
        MusicPanel.NextMusicTrack(1);
    }

    override public void OnToggleMusic(CallbackContext context)
    {
        if(!context.performed || !IsLoaded) { return; }
        if(MainController.MC.MusicPlayer.volume == 0) {
            MusicPanel.RevertMusicVolume();
        } else {
            MusicPanel.SetMusicVolume(0);
        }
    }

    override public void OnToggleVideoSFX(CallbackContext context)
    {
        if(!context.performed || !IsLoaded) { return; }
        if(VideoDeck.GetVolume() == 0) {
            VideoDeck.RevertVolume();
        } else {
            VideoDeck.SetVolume(0);
        }
    }

    override public void OnHideUI(CallbackContext context)
    {
        if(!context.performed) { return; }
        ToggleUI();
    }

    private void ToggleUI()
    {
        if(HelpPanel.IsHelpOpen || !HelpPanel.IsHelpTransitionComplete) { return; }

        var value = !CharacterPanel.isActiveAndEnabled;
        CharacterPanel.gameObject.SetActive(value);
        PosePanel.gameObject.SetActive(value);
        MusicPanel.gameObject.SetActive(value);
        if(ResolutionPicker != null && !MobileLayout.Active) { ResolutionPicker.gameObject.SetActive(value); }
    }

    //Touch: a double tap on the picture (not on a button or list) hides or shows the controls, like the U key
    private const float DoubleTapSeconds = 0.35f;
    private const float DoubleTapRadius = 120f;
    private float lastTapTime = float.NegativeInfinity;
    private Vector2 lastTapPosition;

    //Two fingers: spreading them apart fills the screen with the picture, pinching them together shows it whole again
    private const float PinchOut = 1.2f;
    private const float PinchIn = 0.83f;
    private float pinchStartDistance;
    private bool pinching;
    private bool pinchHandled;

    private bool UpdatePinch(UnityEngine.InputSystem.Touchscreen touch)
    {
        var count = 0;
        Vector2 first = default, second = default;
        foreach(var t in touch.touches)
        {
            if(!t.press.isPressed) { continue; }
            if(count == 0) { first = t.position.ReadValue(); } else if(count == 1) { second = t.position.ReadValue(); }
            count++;
        }

        if(count < 2)
        {
            if(count == 0) { pinching = false; }
            return pinching;   //the second finger lifting first must not count as a tap
        }

        var distance = Vector2.Distance(first, second);
        if(!pinching)
        {
            pinching = true;
            pinchHandled = false;
            pinchStartDistance = distance;
            lastTapTime = float.NegativeInfinity;
        }
        else if(!pinchHandled && pinchStartDistance > 1f)
        {
            var ratio = distance / pinchStartDistance;
            if(ratio >= PinchOut) { VideoDeck.SetFill(true); pinchHandled = true; }
            else if(ratio <= PinchIn) { VideoDeck.SetFill(false); pinchHandled = true; }
        }
        return true;
    }

    private void Update()
    {
        //Computers: a double click on the picture (not on a button or list) switches full screen on or off, like F11
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if(mouse != null && !MobileLayout.Active && mouse.leftButton.wasPressedThisFrame && mouse.clickCount.ReadValue() == 2 && !IsOverControl(mouse.position.ReadValue()))
        {
            ToggleFullscreen();
        }

        var touch = UnityEngine.InputSystem.Touchscreen.current;
        if(touch == null) { return; }
        if(UpdatePinch(touch)) { return; }
        if(!touch.primaryTouch.press.wasPressedThisFrame) { return; }

        var position = touch.primaryTouch.position.ReadValue();
        if(IsOverControl(position)) { lastTapTime = float.NegativeInfinity; return; }

        if(Time.unscaledTime - lastTapTime <= DoubleTapSeconds && (position - lastTapPosition).sqrMagnitude <= DoubleTapRadius * DoubleTapRadius)
        {
            lastTapTime = float.NegativeInfinity;
            ToggleUI();
        }
        else
        {
            lastTapTime = Time.unscaledTime;
            lastTapPosition = position;
        }
    }

    private static bool IsOverControl(Vector2 position)
    {
        if(UnityEngine.EventSystems.EventSystem.current == null) { return false; }
        var hits = new List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { position = position }, hits);
        foreach(var hit in hits)
        {
            if(hit.gameObject.GetComponentInParent<Selectable>() != null || hit.gameObject.GetComponentInParent<ScrollRect>() != null) { return true; }
        }
        return false;
    }

    override public void OnAnyKey(CallbackContext context)
    {
        if(!context.performed || !IsLoaded || !HelpPanel.IsHelpOpen || !HelpPanel.IsHelpTransitionComplete) { return; }
        HelpPanel.OnToggleHelp();
    } 


    override public void OnToggleHelp(CallbackContext context)
    {
        if(!context.performed || !IsLoaded || HelpPanel.IsHelpOpen || !HelpPanel.IsHelpTransitionComplete) { return; }
        HelpPanel.OnToggleHelp();
    } 

    override public void OnFullscreen(CallbackContext context)
    {
        if(!context.performed) { return; }
        ToggleFullscreen();
    }

    private void ToggleFullscreen()
    {
        if(HelpPanel.IsHelpOpen || !HelpPanel.IsHelpTransitionComplete) { return; }
        MainController.MC.ToggleFullScreen(!Screen.fullScreen);
    }

    override public void OnEscape(CallbackContext context)
    {
        if(!context.performed || !IsLoaded || HelpPanel.IsHelpOpen || !HelpPanel.IsHelpTransitionComplete) { return; }
        if(VideoControlsUI.gameObject.activeSelf) { VideoControlsUI.gameObject.SetActive(false); } 
        MainController.MC.GoToScene("Title");
    }
}