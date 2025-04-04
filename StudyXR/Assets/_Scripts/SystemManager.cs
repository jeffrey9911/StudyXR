using UnityEngine;

public class SystemManager : MonoBehaviour
{
    public static SystemManager Instance;


    public GUIManager GUIManager;
    public EnvManager EnvManager;
    public StudyManager StudyManager;
    public WebManager WebManager;
    public AIManager AIManager;
    public SessionManager SessionManager;

    void SetManager()
    {
        GUIManager.SetManager(this);
        EnvManager.SetManager(this);
        StudyManager.SetManager(this);
        WebManager.SetManager(this);
        AIManager.SetManager(this);
        SessionManager.SetManager(this);
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
