using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject optionsPanel;

    [Header("Audio")]
    [SerializeField] private Slider volumeSlider;

    private void Start()
    {
        // Imposta lo slider al volume attuale quando parte il menu
        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
    }

    public void StartGame()
    {
        SceneManager.LoadScene("Intro", LoadSceneMode.Single);
    }

    public void OpenOptions()
    {
        optionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        optionsPanel.SetActive(false);
    }

    public void ExitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
    }
}
