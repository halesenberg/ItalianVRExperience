using UnityEngine;

public class CloseLabels : MonoBehaviour
{
    [SerializeField] private GameObject labelsPanel;

    public void OnCloseButton()
    {
        if (labelsPanel != null)
            labelsPanel.SetActive(false);
    }
}