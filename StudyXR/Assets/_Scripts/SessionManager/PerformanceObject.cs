using UnityEngine;

public class PerformanceObject : MonoBehaviour
{
    public string ObjectName;

    public int RateDeley = 0;

    int DelayCount = 0;

    public string GetObjectData()
    {
        if (DelayCount > 0)
        {
            DelayCount--;
            return null;
        }

        DelayCount = RateDeley;

        return $"{ObjectName},{transform.position.x},{transform.position.y},{transform.position.z}," +
                $"{transform.rotation.eulerAngles.x},{transform.rotation.eulerAngles.y},{transform.rotation.eulerAngles.z}";
    }
}
