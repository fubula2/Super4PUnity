using UnityEngine;
using UnityEngine.UI;

//A button that opens a web page in the default browser
[RequireComponent(typeof(Button))]
public class OpenUrlButton : MonoBehaviour
{
    public string Url;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => Application.OpenURL(Url));
    }
}
