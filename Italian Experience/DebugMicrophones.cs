using UnityEngine;

public class DebugMicrophones : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== AVAILABLE MICROPHONES ===");
        var devices = Microphone.devices;
        for (int i = 0; i < devices.Length; i++)
        {
            Debug.Log($"[{i}] \"{devices[i]}\"");
        }
        Debug.Log("=============================");
    }
}
