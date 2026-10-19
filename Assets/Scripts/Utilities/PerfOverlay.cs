using UnityEngine;
using UnityEngine.InputSystem;

//Small on-screen readout for checking how a device copes: frame rate, slowest frame, memory and the playing clip.
//Toggle with F3 or by touching the screen with three fingers at once.
public class PerfOverlay : MonoBehaviour
{
    private static PerfOverlay instance;
    private bool visible;
    private float windowTime, worst, frames;
    private string line = string.Empty;
    private GUIStyle style;
    private bool touchHeld;

    public static void Install()
    {
        if(instance != null) { return; }
        var go = new GameObject("PerfOverlay");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<PerfOverlay>();
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if(keyboard != null && keyboard.f3Key.wasPressedThisFrame) { visible = !visible; }

        var touch = Touchscreen.current;
        if(touch != null)
        {
            var count = 0;
            foreach(var t in touch.touches) { if(t.press.isPressed) { count++; } }
            if(count >= 3 && !touchHeld) { visible = !visible; touchHeld = true; }
            else if(count == 0) { touchHeld = false; }
        }

        var dt = Time.unscaledDeltaTime;
        windowTime += dt; frames++;
        if(dt > worst) { worst = dt; }
        if(windowTime < 1f) { return; }
        if(!visible) { windowTime = 0; frames = 0; worst = 0; return; }

        var deck = FindFirstObjectByType<VideoDeck>();
        line = string.Format("{0:F0} fps  worst {1:F0} ms  mem {2} MB  ram {3} MB  {4}\n{5}",
            frames / windowTime, worst * 1000f, (System.GC.GetTotalMemory(false) / 1048576), SystemInfo.systemMemorySize, SystemInfo.deviceModel,
            deck != null ? deck.VideoInfo() : "no video");
        windowTime = 0; frames = 0; worst = 0;
    }

    private void OnGUI()
    {
        if(!visible) { return; }
        if(style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(14, Screen.height / 45), normal = { textColor = Color.white } };
        }
        var box = new Rect(10, Screen.height - style.fontSize * 4f, Screen.width - 20, style.fontSize * 3.5f);
        GUI.color = new Color(0, 0, 0, 0.6f);
        GUI.DrawTexture(box, Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(box, line, style);
    }
}
