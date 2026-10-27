using UnityEngine;
using UnityEngine.UI;

public enum RowState { Ready, Queued, Active }

//One series row in the Get Mods list
public class LibraryRow : MonoBehaviour
{
    public Text SeriesText;
    public Text CharactersText;
    public Text UpdatedText;
    public SeriesLink SeriesLink;
    public Button ActionButton;
    public Text ActionLabel;
    public Image ActionImage;

    //Patreon coral-orange
    public static readonly Color PatreonColor = new Color(0.976f, 0.408f, 0.329f);

    [System.NonSerialized] public LibraryEntry Entry;
    private const float IconSize = 48f;
    private const float IconSpace = 62f;   //room taken left of the series name
    private RawImage iconImage;
    private Text iconText;
    private Color defaultColor;
    private Color defaultLabelColor;
    private bool captured;

    public void Bind(LibraryEntry entry)
    {
        Capture();
        Entry = entry;
        BuildIcon();
        iconText.text = entry.IconText ?? string.Empty;
        //Fixed sizes by length (best fit is unreliable with overflow): one letter is big, longer ones shrink to stay in the slot
        iconText.fontSize = iconText.text.Length <= 1 ? 58 : iconText.text.Length == 2 ? 42 : iconText.text.Length == 3 ? 32 : 26;
        iconText.gameObject.SetActive(true);
        iconImage.gameObject.SetActive(false);
        SeriesText.text = entry.Series;
        CharactersText.text = entry.Characters;
        UpdatedText.text = entry.Updated.HasValue ? entry.Updated.Value.ToString("yyyy-MM-dd") : string.Empty;
        SetState(RowState.Ready, 0f);
    }

    //Shows the series logo in place of the letters
    public void SetIcon(Texture2D texture)
    {
        if(texture == null || iconImage == null) { return; }
        iconImage.texture = texture;
        iconImage.gameObject.SetActive(true);
        iconText.gameObject.SetActive(false);
    }

    //The logo (or, without one, the sheet's letters in the Lobster font) sits left of the series name
    private void BuildIcon()
    {
        if(iconImage != null) { return; }
        var series = (RectTransform)SeriesText.transform;
        var left = series.anchoredPosition.x;
        series.anchoredPosition = new Vector2(left + IconSpace, series.anchoredPosition.y);
        series.sizeDelta = new Vector2(series.sizeDelta.x - IconSpace, series.sizeDelta.y);

        RectTransform Slot(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(left, 0f);
            rt.sizeDelta = new Vector2(IconSize, IconSize);
            return rt;
        }

        iconImage = Slot("Icon").gameObject.AddComponent<RawImage>();
        iconImage.raycastTarget = false;

        iconText = Slot("IconText").gameObject.AddComponent<Text>();
        iconText.font = LibraryIcons.Lobster != null ? LibraryIcons.Lobster : SeriesText.font;
        iconText.color = SeriesText.color;
        iconText.alignment = TextAnchor.MiddleCenter;
        iconText.horizontalOverflow = HorizontalWrapMode.Overflow;
        iconText.verticalOverflow = VerticalWrapMode.Overflow;
        iconText.resizeTextForBestFit = false;
        iconText.raycastTarget = false;
        //Taller than the logo slot so a big letter is not shrunk to fit the line height
        ((RectTransform)iconText.transform).sizeDelta = new Vector2(IconSize + 8f, 84f);
        ((RectTransform)iconText.transform).anchoredPosition += new Vector2(-4f, 0f);
    }

    public void SetState(RowState state, float fraction)
    {
        Capture();
        var isPatreon = Entry != null && Entry.Kind == LinkKind.Browser;
        ActionImage.color = isPatreon ? PatreonColor : defaultColor;
        ActionLabel.color = isPatreon ? Color.white : defaultLabelColor;

        switch(state)
        {
            case RowState.Queued: ActionLabel.text = "Queued"; break;
            case RowState.Active: ActionLabel.text = Mathf.RoundToInt(fraction * 100) + "%"; break;
            default: ActionLabel.text = isPatreon ? "Patreon" : "Download"; break;
        }
    }

    private void Capture()
    {
        if(captured) { return; }
        captured = true;
        defaultColor = ActionImage.color;
        defaultLabelColor = ActionLabel.color;
    }
}
