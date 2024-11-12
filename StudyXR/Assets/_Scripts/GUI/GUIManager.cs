using UnityEngine;

public class ControllerPositionState
{
    public Vector3 LeftControllerPosition = new Vector3(0, 0, 0);
    public Vector3 RightControllerPosition = new Vector3(0, 0, 0);

    public void UpdateControllerPosition(Vector3 left, Vector3 right)
    {
        RightControllerPosition = right;
        LeftControllerPosition = left;
    }

    public float GetDScale(Vector3 right, Vector3 left)
    {
        return (Vector3.Distance(left, right) - Vector3.Distance(LeftControllerPosition, RightControllerPosition));
    }

    public Vector3 GetLMove(Vector3 left)
    {
        return (left - LeftControllerPosition);
    }

    public Vector3 GetRMove(Vector3 right)
    {
        return right - RightControllerPosition;
    }

}

public class GUIManager : MonoBehaviour
{
    public GameObject MainCanvas;

    public GameObject ScaleIndicator;
    float ScaleSpeed = 0.001f;
    public GameObject MoveIndicator;
    float MoveSpeed = 1f;

    public ControllerPositionState ControllerPositionState = new ControllerPositionState();
    public Transform LeftHandAnchor;
    public Transform RightHandAnchor;
    public Transform CentreEye;

    bool isDisplayGUI = true;

    Vector3 AnchorPosition = new Vector3(0, 0, 0);
    Vector3 AnchorScale = new Vector3(0, 0, 0);
    Quaternion AnchorRotation = new Quaternion(0, 0, 0, 0);
    float FollowSpeed = 5f;

    void Start()
    {
        AnchorPosition = MainCanvas.transform.position;
        AnchorScale = MainCanvas.transform.localScale;
        AnchorRotation = MainCanvas.transform.rotation;
    }

    void Update()
    {
        UIUpdateAnchor();
        FollowAnchor();
    }

    void UIUpdateAnchor()
    {
        if (OVRInput.GetDown(OVRInput.Button.Start))
        {
            isDisplayGUI = !isDisplayGUI;
            MainCanvas.SetActive(isDisplayGUI);
        }

        if (isDisplayGUI)
        {
            bool isLeftHandTrigger = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger);
            bool isRightHandTrigger = OVRInput.Get(OVRInput.Button.SecondaryHandTrigger);

            if (isLeftHandTrigger || isRightHandTrigger)
            {
                MoveIndicator.SetActive(true);
                ScaleIndicator.SetActive(false);

                if (isLeftHandTrigger && isRightHandTrigger)
                {
                    MoveIndicator.SetActive(false);
                    ScaleIndicator.SetActive(true);

                    float dScale = ControllerPositionState.GetDScale(LeftHandAnchor.localPosition, RightHandAnchor.localPosition);

                    AnchorScale += new Vector3(dScale, dScale, 0) * ScaleSpeed;
                }
                else if (isLeftHandTrigger)
                {
                    AnchorPosition += ControllerPositionState.GetLMove(LeftHandAnchor.localPosition) * MoveSpeed;
                }
                else if (isRightHandTrigger)
                {
                    AnchorPosition += ControllerPositionState.GetRMove(RightHandAnchor.localPosition) * MoveSpeed;
                }
            }
            else
            {
                MoveIndicator.SetActive(false);
                ScaleIndicator.SetActive(false);
            }

            ControllerPositionState.UpdateControllerPosition(LeftHandAnchor.localPosition, RightHandAnchor.localPosition);

            Quaternion lookatRot = Quaternion.LookRotation(CentreEye.position - AnchorPosition, Vector3.up);
            lookatRot *= Quaternion.Euler(0f, 180f, 0f);
            AnchorRotation = lookatRot;
        }
    }

    void FollowAnchor()
    {
        MainCanvas.transform.position = Vector3.Lerp(MainCanvas.transform.position, AnchorPosition, Time.deltaTime * FollowSpeed);
        MainCanvas.transform.rotation = Quaternion.Lerp(MainCanvas.transform.rotation, AnchorRotation, Time.deltaTime * FollowSpeed);
        MainCanvas.transform.localScale = Vector3.Lerp(MainCanvas.transform.localScale, AnchorScale, Time.deltaTime * FollowSpeed);
    }

}
