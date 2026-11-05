using UnityEngine;
using UnityEngine.XR;

public class SempliceTrackingVR : MonoBehaviour
{
    public XRNode nodoHand; // Seleziona LeftHand o RightHand nell'inspector

    void Update()
    {
        // Prende posizione e rotazione dal visore
        InputDevice device = InputDevices.GetDeviceAtXRNode(nodoHand);

        if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos))
            transform.localPosition = pos;

        if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
            transform.localRotation = rot;
    }
}