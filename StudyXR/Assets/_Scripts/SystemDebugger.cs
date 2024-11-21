using TMPro;
using UnityEngine;

public class SystemDebugger : MonoBehaviour
{
    public static SystemDebugger Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public TMP_InputField DebuggerText;

    public void Log(string message)
    {
        DebuggerText.text += message + "\n";
    }
}
