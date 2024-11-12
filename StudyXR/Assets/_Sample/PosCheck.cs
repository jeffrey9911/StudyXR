using UnityEngine;
using UnityEngine.UI;

public class PosCheck : MonoBehaviour
{
    public Transform target1;
    public Transform target2;
    public Transform target3;
    public Transform target4;
    public Transform target5;
    public Transform target6;
    public InputField debugText1;
    public InputField debugText2;
    public InputField debugText3;
    public InputField debugText4;
    public InputField debugText5;
    public InputField debugText6;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        debugText1.text = target1.position.ToString();
        debugText2.text = target2.position.ToString();
        debugText3.text = target3.position.ToString();
        debugText4.text = target4.position.ToString();
        debugText5.text = target5.position.ToString();
        debugText6.text = target6.position.ToString();
    }
}
