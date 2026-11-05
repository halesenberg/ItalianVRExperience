using UnityEngine;

public class PlayerSpawnManager : MonoBehaviour
{
    public static string NextSpawnPointName;

    private void Start()
    {
        if (string.IsNullOrEmpty(NextSpawnPointName)) return;

        GameObject spawnPoint = GameObject.Find(NextSpawnPointName);
        if (spawnPoint == null)
        {
            Debug.LogWarning($"[SpawnManager] Spawn point '{NextSpawnPointName}' non trovato!");
            return;
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("[SpawnManager] Player non trovato!");
            return;
        }

        player.transform.position = spawnPoint.transform.position;
        player.transform.rotation = spawnPoint.transform.rotation;

        NextSpawnPointName = null;
    }
}