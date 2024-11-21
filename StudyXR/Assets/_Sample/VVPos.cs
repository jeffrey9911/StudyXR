using UnityEngine;

public class VVPos : MonoBehaviour
{
    public Transform LeftHand;
    public ControllerPositionState cps = new ControllerPositionState();

    void Update()
    {
        if (OVRInput.Get(OVRInput.Button.Three)) // X
        {
            this.transform.position += cps.GetLMove(LeftHand.position);
        }

        if (OVRInput.Get(OVRInput.Button.Four)) // Y
        {
            float dmove = Vector3.Distance(LeftHand.position, cps.LeftControllerPosition) * 100f;
            this.transform.rotation *= Quaternion.Euler(0, dmove,0);
        }

        cps.UpdateControllerPosition(LeftHand.position, Vector3.zero);
    }
}
