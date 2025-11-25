using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class PlayerSpawnManager_MP : NetworkBehaviour
{
    [Header("Optional dungeon root (Generator)")]
    [SerializeField] private Transform worldRoot;

    private Transform[] hostSpawnPoints;     // objects with SpawnPoint component
    private Transform[] clientSpawnPoints;   // objects with ClientSpawnPoint component

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        StartCoroutine(WaitForDungeonAndFindSpawnPoints());
    }

    private System.Collections.IEnumerator WaitForDungeonAndFindSpawnPoints()
    {
        // Optional: auto-find Generator (not strictly needed, but harmless)
        if (!worldRoot)
        {
            var gen = FindFirstObjectByType<DungeonGenerator_MP>();
            if (gen) worldRoot = gen.transform;
        }

        int guard = 0;
        while ((hostSpawnPoints == null || hostSpawnPoints.Length == 0) &&
               (clientSpawnPoints == null || clientSpawnPoints.Length == 0) &&
               guard < 300)
        {
            // HOST spawns: any object with SpawnPoint component
            var hostMarkers = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
            if (hostMarkers != null && hostMarkers.Length > 0)
            {
                var list = new List<Transform>();
                foreach (var sp in hostMarkers)
                {
                    if (sp != null) list.Add(sp.transform);
                }
                if (list.Count > 0)
                    hostSpawnPoints = list.ToArray();
            }

            // CLIENT spawns: any object with ClientSpawnPoint component
            var clientMarkers = FindObjectsByType<ClientSpawnPoint>(FindObjectsSortMode.None);
            if (clientMarkers != null && clientMarkers.Length > 0)
            {
                var list = new List<Transform>();
                foreach (var sp in clientMarkers)
                {
                    if (sp != null) list.Add(sp.transform);
                }
                if (list.Count > 0)
                    clientSpawnPoints = list.ToArray();
            }

            if ((hostSpawnPoints != null && hostSpawnPoints.Length > 0) ||
                (clientSpawnPoints != null && clientSpawnPoints.Length > 0))
            {
                break;
            }

            guard++;
            yield return null; // wait a frame
        }

        Debug.Log($"[PlayerSpawnManager_MP] Host spawns: {(hostSpawnPoints?.Length ?? 0)}, " +
                  $"Client spawns: {(clientSpawnPoints?.Length ?? 0)}");

        if ((hostSpawnPoints == null || hostSpawnPoints.Length == 0) &&
            (clientSpawnPoints == null || clientSpawnPoints.Length == 0))
        {
            Debug.LogWarning("[PlayerSpawnManager_MP] No SpawnPoint / ClientSpawnPoint components found.");
            yield break;
        }

        // Place players already connected (host included)
        foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
        {
            PlacePlayer(kvp.Key);
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
        base.OnDestroy();
    }

    private void OnClientConnected(ulong clientId)
    {
        PlacePlayer(clientId);
    }

    private void PlacePlayer(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        if (!nm.ConnectedClients.TryGetValue(clientId, out var client))
            return;

        var player = client.PlayerObject;
        if (player == null) return;

        bool isHostClient = clientId == NetworkManager.ServerClientId;

        Transform sp = null;

        if (isHostClient)
        {
            // HOST: use SpawnPoint markers
            if (hostSpawnPoints != null && hostSpawnPoints.Length > 0)
                sp = hostSpawnPoints[0];
            else if (clientSpawnPoints != null && clientSpawnPoints.Length > 0)
                sp = clientSpawnPoints[0]; // fallback
        }
        else
        {
            // CLIENT: use ClientSpawnPoint markers if possible
            if (clientSpawnPoints != null && clientSpawnPoints.Length > 0)
            {
                int idx = (int)((clientId - 1) % (ulong)clientSpawnPoints.Length);
                sp = clientSpawnPoints[idx];
            }
            else if (hostSpawnPoints != null && hostSpawnPoints.Length > 0)
            {
                Debug.LogWarning("[PlayerSpawnManager_MP] No ClientSpawnPoint markers; falling back to SpawnPoint.");
                int idx = (int)(clientId % (ulong)hostSpawnPoints.Length);
                sp = hostSpawnPoints[idx];
            }
        }

        if (!sp)
        {
            Debug.LogWarning($"[PlayerSpawnManager_MP] No spawn transform for client {clientId}.");
            return;
        }

        // Place slightly above ground so the character controller doesn't clip
        var cc = player.GetComponent<CharacterController>();
        Vector3 basePos = sp.position;
        float up = cc ? cc.height * 0.5f : 1.0f;

        Vector3 spawnPos = basePos + Vector3.up * up;
        Quaternion spawnRot = sp.rotation;

        if (cc) cc.enabled = false;
        player.transform.SetPositionAndRotation(spawnPos, spawnRot);
        if (cc) cc.enabled = true;

        Debug.Log($"[PlayerSpawnManager_MP] Spawned client {clientId} at {spawnPos} using {(isHostClient ? "HOST" : "CLIENT")} spawn '{sp.name}'.");
    }
}