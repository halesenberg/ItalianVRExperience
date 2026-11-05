using UnityEngine;

public class RoomZoneTrigger : MonoBehaviour
{
    [SerializeField] private RoomLabelSetup label;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("MainCamera") && !other.CompareTag("Player")) return;
        label.Show();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("MainCamera") && !other.CompareTag("Player")) return;
        label.Hide();
    }
}