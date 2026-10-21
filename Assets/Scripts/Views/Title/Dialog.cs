using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

//Reusable modal: title, body text, up to two buttons, a list of choices, or a checklist
public class Dialog : MonoBehaviour
{
    public GameObject Root;
    public Text TitleText;
    public Text BodyText;
    public Button ButtonA;
    public Button ButtonB;
    public Text LabelA;
    public Text LabelB;
    public GameObject ListScroll;
    public RectTransform ListContent;
    public Button ItemTemplate;

    //Checklist mode
    public Toggle ToggleTemplate;
    public Button ButtonAll;
    public Button ButtonNone;

    private readonly List<GameObject> items = new List<GameObject>();
    private readonly List<Toggle> toggles = new List<Toggle>();
    private static readonly Color CancelRed = new Color(0.85f, 0.12f, 0.12f, 1f);
    private Color defaultCancelColor;
    private Color defaultCancelLabelColor;
    private bool capturedCancel;
    private Func<IReadOnlyList<int>, string> confirmLabelFor;
    private string baseConfirmLabel;
    private bool optionalChecks;   //checkbox-only dialog: confirm works with nothing ticked, and All/None are hidden

    public bool IsOpen => Root.activeSelf;

    public void Show(string title, string body, string labelA, Action onA, string labelB = null, Action onB = null)
    {
        Prepare(title, body, false);
        SizeBox(false);
        ListScroll.SetActive(false);
        Bind(ButtonA, LabelA, labelA, onA);
        Bind(ButtonB, LabelB, labelB, onB);
        CenterButtons();
        Root.SetActive(true);
    }

    //onPick receives the chosen index; the cancel button is ButtonB
    public void ShowList(string title, string body, IReadOnlyList<string> labels, Action<int> onPick, string cancelLabel, Action onCancel)
    {
        Prepare(title, body, true);
        SizeBox(false);
        Bind(ButtonA, LabelA, null, null);
        Bind(ButtonB, LabelB, cancelLabel, onCancel);
        CenterButtons();
        ListScroll.SetActive(true);

        const float itemHeight = 60f;
        for(var i = 0; i < labels.Count; i++)
        {
            var index = i;
            var item = Instantiate(ItemTemplate, ListContent);
            item.gameObject.SetActive(true);
            var rt = (RectTransform)item.transform;
            rt.anchoredPosition = new Vector2(0, -i * (itemHeight + 8));
            Fit(item.GetComponentInChildren<Text>(), labels[i], 10, 14);
            item.onClick.AddListener(() => { Hide(); onPick(index); });
            items.Add(item.gameObject);
        }
        ListContent.sizeDelta = new Vector2(ListContent.sizeDelta.x, labels.Count * (itemHeight + 8));
        ScrollToTop();
        Root.SetActive(true);
    }

    //Confirm gets the ticked indexes and is disabled while none are ticked. Entries start ticked unless initiallyTicked is false.
    //confirmLabelFor (optional) renders the confirm button's text from the current selection.
    public void ShowChecklist(string title, string body, IReadOnlyList<string> labels, string confirmLabel, Action<IReadOnlyList<int>> onConfirm, string cancelLabel, Action onCancel,
        bool initiallyTicked = true, Func<IReadOnlyList<int>, string> confirmLabelFor = null, bool cancelIsRed = false, bool optionalChecks = false)
    {
        Prepare(title, body, true);
        this.optionalChecks = optionalChecks;
        SizeBox(optionalChecks);
        SetCancelRed(cancelIsRed);
        this.confirmLabelFor = confirmLabelFor;
        baseConfirmLabel = confirmLabel;
        ListScroll.SetActive(true);

        const float rowHeight = 52f;
        for(var i = 0; i < labels.Count; i++)
        {
            var toggle = Instantiate(ToggleTemplate, ListContent);
            toggle.gameObject.SetActive(true);
            var rt = (RectTransform)toggle.transform;
            rt.anchoredPosition = new Vector2(0, -i * rowHeight);
            if(optionalChecks)
            {
                //A single checkbox with its text, centered in the (smaller) box
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(340f, rowHeight);
                rt.anchoredPosition = new Vector2(0f, -i * rowHeight);
            }
            Fit(toggle.GetComponentInChildren<Text>(), labels[i], 9, 18);
            toggle.SetIsOnWithoutNotify(initiallyTicked);
            toggle.onValueChanged.AddListener(_ => UpdateConfirm());
            toggles.Add(toggle);
            items.Add(toggle.gameObject);
        }
        ListContent.sizeDelta = new Vector2(ListContent.sizeDelta.x, labels.Count * rowHeight);
        ScrollToTop();

        BindRaw(ButtonA, LabelA, confirmLabel, () =>
        {
            var chosen = Selected();
            Hide();
            onConfirm(chosen);
        });
        LabelA.resizeTextForBestFit = true; LabelA.resizeTextMinSize = 10; LabelA.resizeTextMaxSize = 16;
        Bind(ButtonB, LabelB, cancelLabel, onCancel);
        CenterButtons();

        ButtonAll.gameObject.SetActive(!optionalChecks);
        ButtonNone.gameObject.SetActive(!optionalChecks);
        ButtonAll.onClick.RemoveAllListeners();
        ButtonNone.onClick.RemoveAllListeners();
        ButtonAll.onClick.AddListener(() => SetAll(true));
        ButtonNone.onClick.AddListener(() => SetAll(false));

        UpdateConfirm();
        Root.SetActive(true);
    }

    public void Hide()
    {
        Root.SetActive(false);
        Clear();
    }

    private void SetAll(bool on)
    {
        foreach(var t in toggles) { t.SetIsOnWithoutNotify(on); }
        UpdateConfirm();
    }

    //The cloned scroll view remembers its old position; a list must open at its first entry
    private void ScrollToTop()
    {
        ListContent.anchoredPosition = new Vector2(ListContent.anchoredPosition.x, 0);
        var scroll = ListScroll.GetComponent<ScrollRect>();
        if(scroll == null) { return; }
        Canvas.ForceUpdateCanvases();
        scroll.verticalNormalizedPosition = 1f;
    }

    private List<int> Selected()
    {
        return Enumerable.Range(0, toggles.Count).Where(i => toggles[i].isOn).ToList();
    }

    private void UpdateConfirm()
    {
        var selected = Selected();
        ButtonA.interactable = optionalChecks || selected.Count > 0;
        if(confirmLabelFor != null) { LabelA.text = selected.Count == 0 ? baseConfirmLabel : confirmLabelFor(selected); }
    }

    //Long names shrink to fit their box (two lines at most) instead of running outside it
    private static void Fit(Text text, string value, int minSize, int maxSize)
    {
        text.text = value;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = minSize;
        text.resizeTextMaxSize = maxSize;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
    }

    private static readonly Vector2 SmallBox = new Vector2(760f, 360f);
    private Vector2? normalBox;
    private (bool, bool)? scrollDefaults;

    //Checkbox-only dialogs (tips) get a small box; everything else keeps the usual size
    private void SizeBox(bool small)
    {
        var box = (RectTransform)Root.transform.Find("Box");
        if(box == null) { return; }
        if(normalBox == null) { normalBox = box.sizeDelta; }
        box.sizeDelta = small ? SmallBox : normalBox.Value;

        //One checkbox never needs scrolling: no scroll bars on the small box
        var scroll = ListScroll.GetComponent<ScrollRect>();
        if(scroll != null)
        {
            if(scrollDefaults == null) { scrollDefaults = (scroll.vertical, scroll.horizontal); }
            scroll.vertical = !small && scrollDefaults.Value.Item1;
            scroll.horizontal = !small && scrollDefaults.Value.Item2;
        }
        foreach(Transform child in ListScroll.transform)
        {
            if(child.name.StartsWith("Scrollbar")) { child.gameObject.SetActive(!small); }
        }
    }

    private void Clear()
    {
        optionalChecks = false;
        foreach(var item in items) { Destroy(item); }
        items.Clear();
        toggles.Clear();
        confirmLabelFor = null;
        ButtonA.interactable = true;
        ButtonAll.gameObject.SetActive(false);
        ButtonNone.gameObject.SetActive(false);
    }

    //The cancel button (ButtonB) in red with white text, or back to its normal look
    private void SetCancelRed(bool red)
    {
        var image = ButtonB.GetComponent<Image>();
        if(!capturedCancel)
        {
            capturedCancel = true;
            defaultCancelColor = image.color;
            defaultCancelLabelColor = LabelB.color;
        }
        image.color = red ? CancelRed : defaultCancelColor;
        LabelB.color = red ? Color.white : defaultCancelLabelColor;
    }

    //A single visible button is centered; two sit side by side
    private void CenterButtons()
    {
        var both = ButtonA.gameObject.activeSelf && ButtonB.gameObject.activeSelf;
        Place(ButtonA, both ? -170f : 0f);
        Place(ButtonB, both ? 170f : 0f);
    }

    private static void Place(Button button, float x)
    {
        var rt = (RectTransform)button.transform;
        rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y);
    }

    private const float PlainBodyHeight = 400f;
    private const float ListBodyHeight = 84f;

    //Above a list the intro gets two lines and shrinks to fit; a plain dialog gets the whole box
    private void Prepare(string title, string body, bool aboveList)
    {
        Clear();
        TitleText.text = title;
        BodyText.text = body;
        SetCancelRed(false);

        var rt = (RectTransform)BodyText.transform;
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, aboveList ? ListBodyHeight : PlainBodyHeight);
        BodyText.resizeTextForBestFit = aboveList;
        BodyText.resizeTextMinSize = 11;
        BodyText.resizeTextMaxSize = 17;
        BodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        BodyText.verticalOverflow = aboveList ? VerticalWrapMode.Truncate : VerticalWrapMode.Overflow;
    }

    private void Bind(Button button, Text label, string text, Action action)
    {
        BindRaw(button, label, text, text == null ? (Action)null : () => { Hide(); action?.Invoke(); });
    }

    private static void BindRaw(Button button, Text label, string text, Action onClick)
    {
        button.onClick.RemoveAllListeners();
        button.gameObject.SetActive(text != null);
        if(text == null) { return; }
        label.text = text;
        button.onClick.AddListener(() => onClick());
    }
}
