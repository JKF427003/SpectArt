using System.Collections.Generic;
using UnityEngine;

public class RoomMeta : MonoBehaviour
{
    public enum RoomKind { Normal, Aggro, Safe }
    [Header("Room designation")]
    public RoomKind kind = RoomKind.Normal;

    [Header("Optional: cached spawn points (auto-filled at runtime)")]
    public List<Transform> sitSpawns = new();
    public List<Transform> peepSpawns = new();
    public List<Transform> genericSpawns = new();

    void Awake()
    {
        // Collect by tag; designers can just tag empties in each room prefab
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            if (!t || t == transform) continue;
            switch (t.tag)
            {
                case "EnemySpawn_Sit": sitSpawns.Add(t); break;
                case "EnemySpawn_Peep": peepSpawns.Add(t); break;
                case "EnemySpawn_Generic": genericSpawns.Add(t); break;
            }
        }
    }

    public Transform GetRandomSpawn(string style)
    {
        List<Transform> list = style switch
        {
            "Sit" => sitSpawns,
            "Peep" => peepSpawns,
            _ => genericSpawns
        };
        if (list != null && list.Count > 0)
            return list[Random.Range(0, list.Count)];
        // fallbacks
        if (genericSpawns.Count > 0) return genericSpawns[Random.Range(0, genericSpawns.Count)];
        if (sitSpawns.Count > 0) return sitSpawns[Random.Range(0, sitSpawns.Count)];
        if (peepSpawns.Count > 0) return peepSpawns[Random.Range(0, peepSpawns.Count)];
        return transform; // last resort
    }
}
