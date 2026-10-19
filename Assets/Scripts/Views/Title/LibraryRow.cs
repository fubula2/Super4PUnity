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
    private Color defaultColor;
    private Color defaultLabelColor;
    private bool captured;

    public void Bind(LibraryEntry entry)
    {
        Capture();
        Entry = entry;
        SeriesText.text = entry.Series;
        CharactersText.text = entry.Characters;
        UpdatedText.text = entry.Updated.HasValue ? entry.Updated.Value.ToString("yyyy-MM-dd") : string.Empty;
        SetState(RowState.Ready, 0f);
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
