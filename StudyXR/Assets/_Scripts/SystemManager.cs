using UnityEngine;

public class SystemManager : MonoBehaviour
{
    public static SystemManager Instance;


    public GUIManager GUIManager;
    public EnvManager EnvManager;
    public StudyManager StudyManager;

    void SetManager()
    {
        GUIManager.SetManager(this);
        EnvManager.SetManager(this);
        StudyManager.SetManager(this);
    }

    void Start()
    {
        SetManager();
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
