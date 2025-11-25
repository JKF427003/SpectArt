using UnityEngine;
using Unity.AI.Navigation;

public class NavMeshRuntimeBaker : MonoBehaviour
{
    public DungeonGenerator generator;
    public NavMeshSurface navSurface;

    void Awake()
    {
        // Make sure we have a reference
        if (!generator)
            generator = FindObjectOfType<DungeonGenerator>();

        if (generator != null)
            generator.OnGenerationFinished += BuildMesh;
        else
            Debug.LogError("[NavMeshRuntimeBaker] No DungeonGenerator found.");
    }

    void OnDestroy()
    {
        if (generator != null)
            generator.OnGenerationFinished -= BuildMesh;
    }

    void BuildMesh()
    {
        Debug.Log("Dungeon finished – Baking NavMesh");
        navSurface.BuildNavMesh();
    }
}
