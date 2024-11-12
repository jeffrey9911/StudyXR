using UnityEngine;

public class SystemManager : MonoBehaviour
{
    public static SystemManager Instance;


    public GUIManager GUIManager;
    public SceneManager SceneManager;

    void SetManager()
    {

    }


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
}
