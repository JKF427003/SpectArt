using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Helps find common Netcode setup problems in the current scene.
/// Attach this to any GameObject in GameTest_Multiplayer.
/// </summary>
public class NetworkSceneValidator : MonoBehaviour
{
    void Start()
    {
        CheckNestedNetworkObjects();
        CheckPlayersInScene();
        CheckCacheSeeder();
    }

    void CheckNestedNetworkObjects()
    {
        var all = FindObjectsOfType<NetworkObject>(true);
        foreach (var root in all)
        {
            // Only check objects that are not children of another NetworkObject
            var p = root.transform.parent;
            bool isChildOfNetObj = false;
            while (p != null)
            {
                if (p.GetComponent<NetworkObject>() != null)
                {
                    isChildOfNetObj = true;
                    break;
                }
                p = p.parent;
            }
            if (isChildOfNetObj) continue;

            // Now check if this "root" has nested NetworkObjects
            var children = root.GetComponentsInChildren<NetworkObject>(true);
            if (children.Length > 1)
            {
                Debug.LogError($"[Validator] NESTED NetworkObjects under '{root.name}'. " +
                               $"Root + {children.Length - 1} child NetworkObjects.");
                foreach (var child in children)
                {
                    if (child == root) continue;
                    Debug.LogError($"    -> Child NetworkObject: '{child.name}'", child);
                }
            }
        }
    }

    void CheckPlayersInScene()
    {
        var players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length > 0)
        {
            Debug.LogWarning($"[Validator] There are {players.Length} GameObjects tagged 'Player' in the scene BEFORE Netcode spawning.");
            foreach (var p in players)
                Debug.LogWarning($"    -> Player in scene: {p.name}", p);
        }
    }

    void CheckCacheSeeder()
    {
        var seeder = FindObjectOfType<CacheSeeder>(true);
        if (seeder != null && seeder.isActiveAndEnabled)
        {
            Debug.LogWarning("[Validator] CacheSeeder is ENABLED in this scene. " +
                             "If you expect full offline behaviour, disable this component.");
        }
    }
}