using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class SceneTransitionTrigger : MonoBehaviour
{
    [Header("Impostazioni Scena")]
    [SerializeField] private string sceneToLoad;

    [Header("Spawn Point")]
    [Tooltip("Nome dello spawn point nella scena di destinazione (lascia vuoto per spawn default)")]
    [SerializeField] private string spawnPointName;

    [Header("Filtro Player VR / XR")]
    [SerializeField] private string playerTag = "Player";

    private bool isLoading = false;

    private void Start()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isLoading) return;
        if (IsPlayer(other))
        {
            isLoading = true;
            PlayerSpawnManager.NextSpawnPointName = spawnPointName;
            LoadTargetScene();
        }
    }

    private void LoadTargetScene()
    {
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError($"[SceneTransition] Nessuna scena specificata su '{gameObject.name}'!");
            isLoading = false;
            return;
        }
        Debug.Log($"[SceneTransition] Caricamento scena: {sceneToLoad}");
        SceneManager.LoadScene(sceneToLoad);
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag(playerTag) ||
               other.CompareTag("MainCamera") ||
               other.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor>() != null;
    }
}