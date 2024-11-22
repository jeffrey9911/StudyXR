using UnityEngine;
using TLab;
using TLab.Android.WebView;

public class WebManager : MonoBehaviour
{
    [HideInInspector]
    public SystemManager SystemManager;
    public void SetManager(SystemManager systemManager)
    {
        this.SystemManager = systemManager;
    }
    
    public TLabWebView WebView;

    public void LoadWebPage(string url)
    {
        WebView.LoadUrl(url);
    }
    
}
