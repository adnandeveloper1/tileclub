using UnityEngine;

/// Put on a button. Paste your link in "Url", then call Open() from the Button's On Click.
public class OpenLink : MonoBehaviour
{
    [SerializeField] string url = "https://";

    public void Open()
    {
        if (string.IsNullOrWhiteSpace(url) || url == "https://")
        {
            Debug.LogWarning("OpenLink: Url is empty on " + name);
            return;
        }
        Application.OpenURL(url.Trim());
    }
}