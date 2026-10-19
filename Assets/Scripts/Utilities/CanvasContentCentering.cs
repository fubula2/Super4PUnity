using UnityEngine;
using UnityEngine.UI;

//The menu is laid out for a 16:9 screen. On a wider or taller screen the canvas gets extra width or height, which would
//leave everything pushed to one side. This keeps the laid-out content in a reference-sized area in the middle instead.
public static class CanvasContentCentering
{
    private const float Tolerance = 0.02f;

    public static RectTransform Apply(Canvas canvas)
    {
        var scaler = canvas.GetComponent<CanvasScaler>();
        if(scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize || Screen.height <= 0) { return null; }

        var reference = scaler.referenceResolution;
        var screenAspect = (float)Screen.width / Screen.height;
        var referenceAspect = reference.x / reference.y;
        if(Mathf.Abs(screenAspect / referenceAspect - 1f) < Tolerance) { return null; }

        var holderObject = new GameObject("CenteredContent", typeof(RectTransform));
        var holder = (RectTransform)holderObject.transform;
        holder.SetParent(canvas.transform, false);
        holder.anchorMin = holder.anchorMax = holder.pivot = new Vector2(0.5f, 0.5f);
        holder.sizeDelta = reference;
        holder.anchoredPosition = Vector2.zero;

        //Move the existing children in order; their anchors and sizes are kept, so they now refer to the holder
        var children = new Transform[canvas.transform.childCount];
        for(var i = 0; i < children.Length; i++) { children[i] = canvas.transform.GetChild(i); }
        foreach(var child in children)
        {
            if(child == holder) { continue; }
            child.SetParent(holder, false);
        }
        return holder;
    }
}
