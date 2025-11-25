using UnityEngine;
using System.Collections;

public class EnemyDirector : MonoBehaviour
{
    public static EnemyDirector Instance { get; private set; }

    [Header("References")]
    public Transform player;             // assign in Inspector
    public GameObject enemyPrefab;       // enemy with EnemyController

    [Header("Spawning")]
    public Vector2 spawnIntervalRange = new Vector2(10f, 22f);
    [Range(0f, 1f)] public float spawnChancePerCheck = 0.6f;
    public int maxSimultaneousEnemies = 1; // usually 1 for this style

    RoomMeta currentRoom;
    int aliveEnemies;
    Coroutine spawnLoop;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void OnPlayerEnteredRoom(RoomMeta room)
    {
        currentRoom = room;
        Debug.Log("EnemyDirector: player entered room " + room.name);
        // Tell any existing enemies about the new context (Aggro/Safe)
        BroadcastToEnemies(e => e.OnRoomContextChanged(room));

        if (spawnLoop == null)
            spawnLoop = StartCoroutine(SpawnLoop());
    }

    public void OnPlayerExitedRoom(RoomMeta room)
    {
        if (currentRoom == room)
            currentRoom = null;
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            float wait = Random.Range(spawnIntervalRange.x, spawnIntervalRange.y);
            yield return new WaitForSeconds(wait);

            if (!currentRoom) continue;
            if (aliveEnemies >= maxSimultaneousEnemies) continue;

            if (Random.value > spawnChancePerCheck) continue;

            // Decide style based on available spawn points in this room
            string style = DecideStyle(currentRoom);
            Transform spawn = currentRoom.GetRandomSpawn(style);
            if (!spawn) continue;

            SpawnEnemy(spawn.position, spawn.rotation, style);
        }
    }

    string DecideStyle(RoomMeta room)
    {
        bool hasSit = room.sitSpawns.Count > 0;
        bool hasPeep = room.peepSpawns.Count > 0;

        if (hasSit && hasPeep)
            return Random.value < 0.5f ? "Sit" : "Peep";
        if (hasSit) return "Sit";
        if (hasPeep) return "Peep";
        return "Generic";
    }

    void SpawnEnemy(Vector3 pos, Quaternion rot, string style)
    {
        var go = Instantiate(enemyPrefab, pos, rot);
        var ctrl = go.GetComponent<EnemyController>();
        aliveEnemies++;

        ctrl.Init(player, style, () => aliveEnemies--);
        ctrl.OnRoomContextChanged(currentRoom);
    }

    void BroadcastToEnemies(System.Action<EnemyController> action)
    {
        var enemies = FindObjectsOfType<EnemyController>();
        foreach (var e in enemies)
            action?.Invoke(e);
    }
}
