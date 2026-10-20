using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

//Plays looping pose videos in time with the music. Two players: the current clip loops while the next one is
//prepared in the background; they swap at a loop boundary. Each player renders into its own RenderTexture.
public class VideoDeck : MonoBehaviour
{
    private const float SyncInterval = 0.5f;
    private const double MobileSeekThreshold = 0.35;   //seconds of drift before a phone is worth a seek
    //Phones draw into a smaller buffer (see VideoBufferPolicy)
    private static (int Width, int Height)? bufferSize;
    private static (int Width, int Height) Buffer => bufferSize ??= VideoBufferPolicy.For(SystemInfo.systemMemorySize, MobileLayout.Active);   //first used after Awake: SystemInfo cannot run in a field initializer
    private static int DefaultWidth => Buffer.Width;
    private static int DefaultHeight => Buffer.Height;

    private class Slot
    {
        public VideoPlayer Player;
        public RenderTexture Texture;
        public RawImage Image;
        public float Aspect = 16f / 9f;   //the clip's shape; the picture is sized and centred from it (see Layout)
        public string Path;
        public bool Ready;
        public bool Warming;         //phones: playing silently just long enough to show the first frame
        public bool Warm;            //paused on its first frame, ready to start with no delay
        public double KnownLength;   //captured when prepared; the player reports 0 after an error
        public double LastTime;      //last playback time seen while playing
        public double Length => Player.isPrepared && Player.length > 0 ? Player.length : KnownLength;
    }

    public event Action OnReady;
    public event Action OnCycleComplete;
    public event Action<string> OnClipFailed;

    public bool IsLoaded { get; private set; }

    //Clips whose last frames cannot be decoded (the player errors instead of reaching the end); remembered for the session
    private static readonly HashSet<string> BadEnd = new HashSet<string>();

    private Slot current;
    private Slot next;
    private bool holding;
    private double holdRemaining;
    private AudioSource musicClock;
    private string queuedPath;
    private bool retiring;
    private float syncTimer;
    private float volume = 1f;
    private float prevVolume = 1f;

    public void Initialize(AudioSource clock)
    {
        musicClock = clock;
        IsLoaded = false;

        var canvasObj = new GameObject("VideoDeckCanvas", typeof(RectTransform), typeof(Canvas));
        canvasObj.transform.SetParent(transform, false);
        var canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -100;

        current = CreateSlot("VideoA", canvasObj.transform);
        next = CreateSlot("VideoB", canvasObj.transform);
        volume = GameDataManager.GM.PlyrConfigData.VideoVolume;
        prevVolume = volume;
        SetFill(MobileLayout.Active && GameDataManager.GM.PlyrConfigData.VideoFillScreen, false);
    }

    public bool FillScreen { get; private set; }

    //Phones: the picture fills the whole screen (cropped at the edges) or is shown whole, centred, with bars. Computers keep full height.
    public void SetFill(bool fill, bool save = true)
    {
        FillScreen = fill;
        Layout(current);
        Layout(next);
        if(save)
        {
            GameDataManager.GM.PlyrConfigData.VideoFillScreen = fill;
            GameDataManager.GM.SavePlyrCfgData();
        }
    }

    //The whole picture is always shown centred, so a resized or maximized window never crops it; phones can zoom to fill
    private VideoFit Fit => MobileLayout.Active && FillScreen ? VideoFit.Fill : VideoFit.Whole;

    //Sets the picture's size and puts it in the exact middle of the screen
    private void Layout(Slot slot)
    {
        if(slot == null) { return; }
        var rect = slot.Image.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        var (width, height) = VideoLayout.SizeFor(Screen.width, Screen.height, slot.Aspect, Fit);
        rect.sizeDelta = new Vector2(width, height);
    }

    private Slot CreateSlot(string slotName, Transform canvasTransform)
    {
        var slot = new Slot();

        var imageObj = new GameObject(slotName, typeof(RectTransform), typeof(RawImage));
        imageObj.transform.SetParent(canvasTransform, false);

        slot.Image = imageObj.GetComponent<RawImage>();
        slot.Image.enabled = false;
        slot.Aspect = (float)DefaultWidth / DefaultHeight;

        slot.Texture = new RenderTexture(DefaultWidth, DefaultHeight, 0);
        slot.Image.texture = slot.Texture;

        slot.Player = imageObj.AddComponent<VideoPlayer>();
        slot.Player.playOnAwake = false;
        slot.Player.isLooping = true;
        slot.Player.skipOnDrop = true;
        slot.Player.audioOutputMode = VideoAudioOutputMode.Direct;
        slot.Player.renderMode = VideoRenderMode.RenderTexture;
        slot.Player.targetTexture = slot.Texture;
        slot.Player.prepareCompleted += OnPrepared;
        slot.Player.loopPointReached += OnLoopPoint;
        slot.Player.errorReceived += OnError;
        return slot;
    }

    //Request a clip. First call fills the current slot; later calls queue the clip to play after the current loop.
    public void LoadNextVideo(string videoPath)
    {
        if(current.Path == null) { StartPrepare(current, videoPath); return; }

        //Already playing it: keep looping
        if(videoPath == current.Path) { queuedPath = null; return; }

        queuedPath = videoPath;
        TryPrepareQueued();
    }

    public void PlayVideos()
    {
        if(current.Player.isPlaying || !current.Ready) { return; }

        current.Image.enabled = true;
        current.Player.Play();
    }

    private void StartPrepare(Slot slot, string path)
    {
        slot.Player.frameReady -= OnWarmFrame;
        slot.Player.sendFrameReadyEvents = false;
        slot.Player.Stop();
        slot.Ready = false;
        slot.Warming = false;
        slot.Warm = false;
        slot.Path = path;
        slot.Player.url = LinuxVideoPrep.Needed ? LinuxVideoPrep.Resolve(path) : path;
        slot.Player.Prepare();
    }

    private void TryPrepareQueued()
    {
        if(retiring || queuedPath == null || next.Path == queuedPath) { return; }
        StartPrepare(next, queuedPath);
    }

    private Slot SlotOf(VideoPlayer player)
    {
        return player == current.Player ? current : player == next.Player ? next : null;
    }

    private void OnPrepared(VideoPlayer player)
    {
        var slot = SlotOf(player);
        if(slot == null) { return; }

        var width = (int)player.width;
        var height = (int)player.height;
        if(width > 0 && height > 0)
        {
            if(slot.Texture.width != width || slot.Texture.height != height)
            {
                player.targetTexture = null;
                slot.Texture.Release();
                slot.Texture.width = width;
                slot.Texture.height = height;
                player.targetTexture = slot.Texture;
            }
            slot.Aspect = (float)width / height;
            Layout(slot);
        }

        slot.Ready = true;
        slot.KnownLength = player.length;
        ApplyVolume(player);

        //Phones start the queued clip's decoder early, silently, so that switching to it needs no start-up wait
        if(slot == next && MobileLayout.Active && !retiring)
        {
            slot.Warming = true;
            if(player.audioTrackCount > 0) { player.SetDirectAudioMute(0, true); }
            player.sendFrameReadyEvents = true;
            player.frameReady += OnWarmFrame;
            player.Play();
        }

        if(slot == current && !IsLoaded)
        {
            IsLoaded = true;
            OnReady?.Invoke();
        }
    }

    private void OnWarmFrame(VideoPlayer player, long frame)
    {
        player.frameReady -= OnWarmFrame;
        player.sendFrameReadyEvents = false;
        var slot = SlotOf(player);
        if(slot == null || !slot.Warming) { return; }

        player.Pause();
        slot.Warming = false;
        slot.Warm = true;
    }

    private bool NextIsReadyToSwap()
    {
        if(queuedPath == null || !next.Ready || next.Path != queuedPath) { return false; }
        return !MobileLayout.Active || next.Warm;
    }

    private void OnError(VideoPlayer player, string message)
    {
        var slot = SlotOf(player);
        if(slot == null) { return; }

        var failedPath = slot.Path;

        if(slot == current)
        {
            //The player errors instead of reporting the end when the last frames cannot be decoded. If that happens at
            //the end of the clip, it simply ended: move on, and pause just before the end next time.
            var atEnd = ClipEndPolicy.IsErrorAtEnd(slot.LastTime, slot.Length);
            Debug.LogWarning("Video error " + (atEnd ? "at the end of" : "during") + " playback: " + failedPath + " (" + message + ")");
            if(failedPath != null) { BadEnd.Add(failedPath); }
            if(!atEnd) { OnClipFailed?.Invoke(failedPath); }

            //Already pausing before the end: the hold finishes the clip
            if(!holding) { FinishClip(true); }
            return;
        }

        Debug.LogWarning("Video failed: " + failedPath + " (" + message + ")");

        //A failed queued clip is dropped; the current clip keeps looping
        if(queuedPath == failedPath) { queuedPath = null; }
        slot.Path = null;
        slot.Ready = false;
        OnClipFailed?.Invoke(failedPath);
    }

    //The current clip is over (it errored at its end, or a known-bad one was paused just before it): switch to the
    //queued clip if it is ready, otherwise play the same clip again
    private void FinishClip(bool errored)
    {
        holding = false;
        if(NextIsReadyToSwap())
        {
            Swap();
        }
        else if(errored)
        {
            current.Player.Stop();
            current.Player.Play();
        }
        else
        {
            current.Player.time = 0;
            current.Player.Play();
        }
        OnCycleComplete?.Invoke();
    }

    private void OnLoopPoint(VideoPlayer player)
    {
        if(player != current.Player) { return; }

        if(NextIsReadyToSwap())
        {
            Swap();
        }
        else
        {
            ResyncAtBoundary();
        }

        OnCycleComplete?.Invoke();
    }

    private void Swap()
    {
        var old = current;
        current = next;
        next = old;
        queuedPath = null;
        retiring = true;

        var bar = VideoSync.BarLength(current.Length);
        var phase = VideoSync.Phase(musicClock.time, bar);
        SetSpeed(1f);

        //Every seek makes a phone's player stall for a moment, so phones skip the small catch-up seek
        var seekFrom = MobileLayout.Active ? MobileSeekThreshold : 0.05;
        if(phase > seekFrom && phase < bar / 2) { current.Player.time = phase; }

        current.Image.enabled = true;
        current.Image.transform.SetAsLastSibling();

        if(current.Warm)
        {
            //Already showing its first frame: start it, unmute it, and let the old clip go straight away
            current.Warm = false;
            if(current.Player.audioTrackCount > 0) { current.Player.SetDirectAudioMute(0, false); }
            current.Player.Play();
            RetireOld();
            return;
        }

        current.Player.sendFrameReadyEvents = true;
        current.Player.frameReady += OnFirstFrame;
        current.Player.Play();
    }

    private void RetireOld()
    {
        next.Image.enabled = false;
        next.Player.Stop();
        next.Ready = false;
        next.Path = null;
        retiring = false;
        TryPrepareQueued();
    }

    //Once the new clip shows a frame, the old one can be hidden and its player reused
    private void OnFirstFrame(VideoPlayer player, long frame)
    {
        player.frameReady -= OnFirstFrame;
        player.sendFrameReadyEvents = false;
        RetireOld();
    }

    private void ResyncAtBoundary()
    {
        if(!musicClock.isPlaying) { return; }

        var bar = VideoSync.BarLength(current.Length);
        var drift = VideoSync.Drift(current.Player.time, musicClock.time, bar);
        var correction = VideoSync.Decide(drift, bar, current.Player.time);
        //Small drift is left alone on phones: a seek stalls their player for longer than the drift is noticeable
        if(MobileLayout.Active && Math.Abs(drift) < MobileSeekThreshold) { return; }
        if(correction.Kind == SyncKind.Seek) { current.Player.time = correction.SeekTime; }
    }

    private int laidOutWidth, laidOutHeight;

    private void Update()
    {
        if(current == null) { return; }

        //Window resized or phone turned: size and centre the picture again
        if(Screen.width != laidOutWidth || Screen.height != laidOutHeight)
        {
            laidOutWidth = Screen.width; laidOutHeight = Screen.height;
            Layout(current);
            Layout(next);
        }

        if(holding)
        {
            holdRemaining -= Time.unscaledDeltaTime;
            if(holdRemaining <= 0) { FinishClip(false); }
            return;
        }

        if(current.Player.isPlaying)
        {
            current.LastTime = current.Player.time;
            if(current.Path != null && ClipEndPolicy.ShouldHoldEarly(BadEnd.Contains(current.Path), current.LastTime, current.Length))
            {
                //Pause on the last good frame; the real end comes after the remaining time
                current.Player.Pause();
                holding = true;
                holdRemaining = ClipEndPolicy.HoldSeconds(current.LastTime, current.Length);
                return;
            }
        }

        if(!current.Player.isPlaying || !musicClock.isPlaying) { return; }

        syncTimer += Time.unscaledDeltaTime;
        if(syncTimer < SyncInterval) { return; }
        syncTimer = 0;

        var bar = VideoSync.BarLength(current.Length);
        var drift = VideoSync.Drift(current.Player.time, musicClock.time, bar);
        var correction = VideoSync.Decide(drift, bar, current.Player.time);
        //Phones skip the speed nudges: every speed change is a call into the system player, which costs more than the small drift it fixes
        if(MobileLayout.Active && correction.Kind == SyncKind.Nudge) { correction = new SyncCorrection(SyncKind.None, 1f, 0); }
        switch(correction.Kind)
        {
            case SyncKind.None: SetSpeed(1f); break;
            case SyncKind.Nudge: SetSpeed(correction.PlaybackSpeed); break;
            //Large drift is fixed with one seek at the next loop boundary; ease towards it meanwhile
            case SyncKind.Seek: SetSpeed(drift > 0 ? 1f - VideoSync.NudgeSpeed : 1f + VideoSync.NudgeSpeed); break;
        }
    }

    //Only talks to the video player when the speed really changes
    private void SetSpeed(float speed)
    {
        if(!Mathf.Approximately(current.Player.playbackSpeed, speed)) { current.Player.playbackSpeed = speed; }
    }

    //For the on-screen readout: the clip's own size and frame rate against the buffer it is drawn into
    public string VideoInfo()
    {
        if(current == null || current.Path == null) { return "no clip"; }
        var p = current.Player;
        return string.Format("clip {0}x{1} @{2:F0}fps  buffer {3}x{4}  t={5:F2}/{6:F2}  speed {7:F2}  {8}",
            p.width, p.height, p.frameRate, current.Texture.width, current.Texture.height, p.time, current.Length, p.playbackSpeed, p.isPlaying ? "playing" : "paused");
    }

    public double CurrentTime => current != null ? current.Player.time : -1;
    public bool CurrentPlaying => current != null && current.Player.isPlaying;

    public string Diagnostics()
    {
        if(current == null) { return "no deck"; }
        return string.Format("cur[t={0:F2} frame={1} playing={2} prepared={3} speed={4:F2}] next[path={5} ready={6} prepared={7} playing={8}] retiring={9} queued={10} loaded={11}",
            current.Player.time, current.Player.frame, current.Player.isPlaying, current.Player.isPrepared, current.Player.playbackSpeed,
            next.Path != null, next.Ready, next.Player.isPrepared, next.Player.isPlaying, retiring, queuedPath != null, IsLoaded);
    }

    public string DebugState()
    {
        if(current == null || current.Path == null) { return "idle"; }
        var drift = VideoSync.Drift(current.Player.time, musicClock.time, VideoSync.BarLength(current.Length));
        return string.Format("playing={0} t={1:F2}/{2:F2} speed={3:F2} drift={4:F3} queued={5}",
            current.Player.isPlaying, current.Player.time, current.Length, current.Player.playbackSpeed, drift, queuedPath != null);
    }

    public float GetVolume()
    {
        return volume;
    }

    public void SetVolume(float newVolume)
    {
        prevVolume = volume;
        volume = newVolume;

        if(current == null) { return; }
        ApplyVolume(current.Player);
        ApplyVolume(next.Player);

        GameDataManager.GM.PlyrConfigData.VideoVolume = newVolume;
    }

    public void RevertVolume()
    {
        SetVolume(prevVolume);
    }

    private void ApplyVolume(VideoPlayer player)
    {
        if(player.audioTrackCount > 0) { player.SetDirectAudioVolume(0, volume); }
    }

    private void OnDestroy()
    {
        if(current == null) { return; }
        current.Texture.Release();
        next.Texture.Release();
    }
}
