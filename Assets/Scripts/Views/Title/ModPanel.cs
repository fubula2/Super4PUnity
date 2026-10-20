using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

//Panel that lists all Mods in Mod Folder
public class ModPanel : MonoBehaviour
{
    public SourcePanel SourcePanel;
    public GameObject ContentList;
    public GameObject ModPreviewPanel;
    public ObjectPooler ItemPooler;
    public Text ModLabelTxt;
    public Text NoModsMsg;
    public InputField SearchInput;
    //public Text ModCountTxt;
    private int ModCount;
    private List<ModListItem> ContentListItems;
    private float InitialContentListHeight;
    private float InitialContentListOffsetMinY;
    
    void Start() {
        ContentListItems = new List<ModListItem>();
    }

    public void Initialize() 
    {
        ModCount = 0;

        var contentListRect = ContentList.GetComponent<RectTransform>();
        InitialContentListHeight = contentListRect.rect.height;
        InitialContentListOffsetMinY = contentListRect.offsetMin.y;

        var charDirs = GameDataManager.GM.CharDirs;
        var index = 0;

        foreach(var charDir in charDirs)
        {
            //TODO: Should we display long name or short name?
            
            var modPath = Path.GetFileName(charDir.TrimEnd('/', '\\')).Substring("CHAR_".Length);

            var modShortName = modPath.Substring(modPath.LastIndexOf('_') + 1);

            SpriteLibrary.SL.AddExternalSprite(MediaFiles.IconIn(charDir, modShortName), modShortName);

            var modListItem = ItemPooler.GetPooledObject("ModListItem", ContentList.transform).GetComponent<ModListItem>();
            modListItem.ModName = modShortName;
            modListItem.ModPath = modPath;
            modListItem.Label.text = modPath + (MediaFiles.VideosIn(charDir).Count == 0 ? "  (no videos)" : string.Empty);
            modListItem.transform.localPosition = new Vector3(0, -45f * index, 0);
            modListItem.Toggle.isOn = false;
            modListItem.ModPreviewPanel = ModPreviewPanel;

            modListItem.Toggle.onValueChanged.AddListener(delegate { 
                OnItemSelect(modListItem); });
            ContentListItems.Add(modListItem);

            index++;
        }

        if(index == 0) {
            NoModsMsg.text = NoModsMsg.text + GameDataManager.GM.GetModsPath().CleanFolderPath();
            NoModsMsg.gameObject.SetActive(true);
        }

        ResizeContentList(index);

        SearchInput.SetTextWithoutNotify(string.Empty);
        SearchInput.onValueChanged.RemoveListener(FilterMods);
        SearchInput.onValueChanged.AddListener(FilterMods);
    }

    private void ResizeContentList(int visibleCount)
    {
        var contentListRect = ContentList.GetComponent<RectTransform>();
        var heightDiff = Mathf.Max(0f, visibleCount * 45f - InitialContentListHeight);
        contentListRect.offsetMin = new Vector2(contentListRect.offsetMin.x, InitialContentListOffsetMinY - heightDiff);
    }

    //Show only mods matching the search text; ticked mods stay ticked while hidden
    public void FilterMods(string searchText)
    {
        var visibleCount = 0;
        foreach(var item in ContentListItems)
        {
            var isMatch = ModFilter.Matches(item.ModPath, searchText);
            item.gameObject.SetActive(isMatch);
            if(isMatch)
            {
                item.transform.localPosition = new Vector3(0, -45f * visibleCount, 0);
                visibleCount++;
            }
        }
        ResizeContentList(visibleCount);
    }

    private void ClearSearch()
    {
        if(string.IsNullOrEmpty(SearchInput.text)) { return; }
        SearchInput.SetTextWithoutNotify(string.Empty);
        FilterMods(string.Empty);
    }

    public void OnItemSelect(ModListItem modListItem, bool isFromPreset = false)
    {
        ModCount += modListItem.Toggle.isOn ? 1 : -1;
        ModLabelTxt.text = "Chars - " + ModCount;
        //ModCountTxt.text = ModCount + " selected";

        if(!isFromPreset) { SourcePanel.OnModToggle(modListItem.Toggle.isOn, modListItem.ModPath, true); }
        SourcePanel.CheckPlayBtn();
    }

    public void OnSelectAll()
    {
        foreach(var item in ContentListItems)
        {
            if(item.gameObject.activeSelf && !item.Toggle.isOn) { item.Toggle.isOn = true; }
        }
    }

    public void OnClear(bool isFromPreset = false)
    {
        foreach(var item in ContentListItems)
        {
            if(!isFromPreset && !item.gameObject.activeSelf) { continue; }
            if(item.Toggle.isOn) { 
                if(isFromPreset) { 
                    item.Toggle.SetIsOnWithoutNotify(false);
                    OnItemSelect(item, isFromPreset);
                }
                else { item.Toggle.isOn = false; }
            }
        }
    }

    //Invoked by SourcePanel when changing source
    //True = All mods found. False = Some mods not found
    public bool SetModsByString(string modListStr)
    {
        ClearSearch();
        OnClear(true);
        var allModsFound = true;

        foreach(var modPath in modListStr.Split(','))
        {
            //TODO: If don't find a matching mod, put up a warning message
            var item = ContentListItems.FirstOrDefault(x => x.ModPath == modPath.Trim());
            if(item != null)
            {
                item.Toggle.SetIsOnWithoutNotify(true);
                OnItemSelect(item, true);
            }
            //Remove "broken" reference
            else
            { 
                if(allModsFound) { 
                    allModsFound = false; 
                }
                SourcePanel.OnModToggle(false, modPath, true);
            }
        }

        return allModsFound;
    }

    //Call before leaving scene to clear unused mod sprites
    public void RemoveUnusedSprites()
    {
        SpriteLibrary.SL.RemoveSpritesByName(ContentListItems.Where(x => !x.Toggle.isOn).Select(x => x.ModName));
    }
}