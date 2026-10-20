using UnityEngine;
using UnityEngine.UI;

//Window size picker shown at the top centre of the player (computers only). It hides and shows with the other controls (U).
public class ResolutionPicker : MonoBehaviour
{
    public Dropdown Dropdown;

    private void Awake()
    {
        if(MobileLayout.Active) { gameObject.SetActive(false); return; }

        //The dropdown was copied from the menu; its saved handler points at the menu, so it gets a fresh event
        Dropdown.onValueChanged = new Dropdown.DropdownEvent();
        Dropdown.onValueChanged.AddListener(OnPick);
    }

    private void OnEnable()
    {
        if(Dropdown == null || GameDataManager.GM == null || GameDataManager.GM.PlyrConfigData == null) { return; }
        Dropdown.SetValueWithoutNotify((int)GameDataManager.GM.PlyrConfigData.AspectResolution);
        Dropdown.RefreshShownValue();
    }

    //Picking a size leaves full screen and sets the window to that size
    private void OnPick(int index)
    {
        GameDataManager.GM.PlyrConfigData.IsFullscreen = false;
        MainController.MC.SwitchResolution(index);
        GameDataManager.GM.SavePlyrCfgData();
    }
}
