using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public GameObject playerPrefab;
    public Transform worldRoot;

    private void Start()
    {
        SpawnPoint spawn = worldRoot ? worldRoot.GetComponentInChildren<SpawnPoint>(true) : FindAnyObjectByType<SpawnPoint>();

        if (!spawn)
        {
            Debug.LogError("[SpectArt] No SpawnPoint found. Dropping player at origin.");
            Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            return;
        }

        var p = Instantiate(playerPrefab, spawn.transform.position, spawn.transform.rotation);
        p.name = "Player";

        var cam = p.GetComponentInChildren<Camera>();
        if (cam && cam.tag != "MainCamera") cam.tag = "MainCamera";
    }
}
