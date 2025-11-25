using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

public class DungeonGenerator_MP : NetworkBehaviour
{
    // -------- grid cell ----------
    class Cell
    {
        public bool visited;                 // carved by DFS
        public bool occupied;                // covered by a placed room footprint
        public bool[] status = new bool[4];  // 0 up,1 down,2 right,3 left (openings)
        public RoomBehaviour rb;             // instance (on anchor only)
        public Vector2Int footprint = Vector2Int.one;
        public int[] socketIndex = new int[4]; // chosen socket per side (paired later)
    }

    [System.Serializable]
    public class RoomDefinition
    {
        public GameObject prefab;
        [Tooltip("Exact number required across the whole dungeon.")]
        public int requiredCount = 0;
        [Tooltip("If true, this prefab may be used to fill remaining cells.")]
        public bool allowAsFiller = true;
        [Tooltip("Relative chance among other filler rooms.")]
        public float randomWeight = 1f;
    }

    [Header("Maze grid (in cells)")]
    public Vector2Int size = new Vector2Int(10, 10);
    public int startPos = 0;

    [Header("Sparsity (stop carving early like your original)")]
    [Range(0.15f, 1f)] public float targetFill = 0.55f;

    [Header("World spacing per cell")]
    public Vector2 cellSize = new Vector2(12, 12);

    [Header("Placement control")]
    [Tooltip("If we cannot satisfy all required counts, we regenerate a whole layout up to this many tries.")]
    public int maxRegenerateAttempts = 20;

    [Header("Room prefabs + counts")]
    public List<RoomDefinition> roomTypes = new List<RoomDefinition>();

    List<Cell> board;
    Dictionary<RoomDefinition, int> placed; // counts per type

    //void Start()
    //{
    //    if (!IsServer) return;
    //    GenerateWithRetries();
    //}

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;
        GenerateWithRetries();
    }

    // ============================================================
    // Top-level retry loop (keeps layouts connected & respects counts)
    // ============================================================
    void GenerateWithRetries()
    {
        for (int attempt = 1; attempt <= Mathf.Max(1, maxRegenerateAttempts); attempt++)
        {
            ClearChildren();

            if (TryGenerateOnce())
            {
                // success
                // Debug.Log($"Generation succeeded in {attempt} attempt(s).");
                return;
            }
            // else try again with a fresh layout
        }

        Debug.LogWarning("Dungeon: failed to satisfy all required counts after max attempts.");
    }

    bool TryGenerateOnce()
    {
        // init grid
        board = new List<Cell>(size.x * size.y);
        for (int i = 0; i < size.x * size.y; i++) board.Add(new Cell());

        // carve connected maze (DFS) with early stop
        CarveDFS();

        // place rooms: strict required first, then fillers
        placed = roomTypes.ToDictionary(r => r, r => 0);
        if (!PlaceRequiredRooms()) return false;

        PlaceFillers();

        // pair openings and apply visuals
        FinalizeEntrances();

        // verify counts
        foreach (var def in roomTypes)
            if (def.requiredCount > 0 && placed[def] < def.requiredCount)
                return false;

        return true;
    }

    // ============================================================
    // DFS carving (connected component from start)
    // ============================================================
    void CarveDFS()
    {
        int total = size.x * size.y;
        int targetVisited = Mathf.Clamp(Mathf.RoundToInt(total * targetFill), 1, total);

        int current = Mathf.Clamp(startPos, 0, total - 1);
        Stack<int> path = new Stack<int>();
        int visited = 0;
        int guard = 0;

        while (guard++ < 100000)
        {
            if (!board[current].visited)
            {
                board[current].visited = true;
                visited++;
                if (visited >= targetVisited) break;
            }

            var neighbors = UnvisitedNeighbors(current);
            if (neighbors.Count == 0)
            {
                if (path.Count == 0) break;
                current = path.Pop();
                continue;
            }

            path.Push(current);
            int next = neighbors[Random.Range(0, neighbors.Count)];

            // open walls between current and next
            if (next == current + 1) { board[current].status[2] = true; current = next; board[current].status[3] = true; }
            else if (next == current - 1) { board[current].status[3] = true; current = next; board[current].status[2] = true; }
            else if (next == current + size.x) { board[current].status[1] = true; current = next; board[current].status[0] = true; }
            else if (next == current - size.x) { board[current].status[0] = true; current = next; board[current].status[1] = true; }
        }
    }

    List<int> UnvisitedNeighbors(int idx)
    {
        var list = new List<int>();
        int w = size.x, h = size.y;
        int x = idx % w, y = idx / w;

        int up = (y > 0) ? idx - w : -1;
        int down = (y < h - 1) ? idx + w : -1;
        int right = (x < w - 1) ? idx + 1 : -1;
        int left = (x > 0) ? idx - 1 : -1;

        if (up >= 0 && !board[up].visited) list.Add(up);
        if (down >= 0 && !board[down].visited) list.Add(down);
        if (right >= 0 && !board[right].visited) list.Add(right);
        if (left >= 0 && !board[left].visited) list.Add(left);

        return list;
    }

    // ============================================================
    // Placement
    // ============================================================
    bool PlaceRequiredRooms()
    {
        // candidate anchors = visited cells not yet occupied
        var anchors = new List<int>();
        for (int i = 0; i < board.Count; i++)
            if (board[i].visited && !board[i].occupied)
                anchors.Add(i);
        Shuffle(anchors);

        // place each required type
        foreach (var def in roomTypes.Where(r => r.requiredCount > 0))
        {
            int need = def.requiredCount;

            for (int a = 0; a < anchors.Count && need > 0; a++)
            {
                int idx = anchors[a];
                if (idx < 0) continue;

                int x = idx % size.x, y = idx / size.x;
                if (!SupportsSidesAndFits(def, x, y)) continue;

                Place(def, x, y, out Vector2Int fp);
                placed[def]++;

                // reserve this footprint and remove its cells from anchors
                ReserveFootprint(x, y, fp);
                RemoveFootprintFromList(anchors, x, y, fp);

                need--;
                anchors[a] = -1; // consumed
            }

            if (need > 0) return false; // fail this attempt → regenerate
        }

        return true;
    }

    void PlaceFillers()
    {
        var anchors = new List<int>();
        for (int i = 0; i < board.Count; i++)
            if (board[i].visited && !board[i].occupied)
                anchors.Add(i);
        Shuffle(anchors);

        for (int a = 0; a < anchors.Count; a++)
        {
            int idx = anchors[a];
            if (idx < 0) continue;
            int x = idx % size.x, y = idx / size.x;

            var pool = roomTypes.Where(r => r.allowAsFiller && SupportsSidesAndFits(r, x, y)).ToList();
            if (pool.Count == 0) continue;

            float sum = pool.Sum(r => Mathf.Max(0.0001f, r.randomWeight));
            float v = Random.value * sum;
            RoomDefinition pick = pool[^1];
            foreach (var r in pool)
            {
                v -= Mathf.Max(0.0001f, r.randomWeight);
                if (v <= 0f) { pick = r; break; }
            }

            Place(pick, x, y, out Vector2Int fp);
            placed[pick]++;
            ReserveFootprint(x, y, fp);
            RemoveFootprintFromList(anchors, x, y, fp);
            anchors[a] = -1;
        }
    }

    bool SupportsSidesAndFits(RoomDefinition def, int x, int y)
    {
        if (!def.prefab) return false;
        var rb = def.prefab.GetComponent<RoomBehaviour>();
        if (!rb) return false;

        // must support each open side of the anchor
        var cell = board[x + y * size.x];
        for (int s = 0; s < 4; s++)
            if (cell.status[s] && rb.SocketCount(s) == 0)
                return false;

        // footprint must fit and must not cover carved corridors
        var g = rb.gridSize;
        if (g.x < 1 || g.y < 1) g = Vector2Int.one;

        if (x + g.x - 1 >= size.x || y + g.y - 1 >= size.y) return false;

        for (int dy = 0; dy < g.y; dy++)
            for (int dx = 0; dx < g.x; dx++)
            {
                int cx = x + dx, cy = y + dy;
                var c = board[cx + cy * size.x];

                if (c.occupied) return false;

                // Don't let footprints cover through-corridors
                if (!(dx == 0 && dy == 0) && (c.status[0] || c.status[1] || c.status[2] || c.status[3]))
                    return false;
            }

        return true;
    }

    void Place(RoomDefinition def, int x, int y, out Vector2Int fp)
    {
        var rbPrefab = def.prefab.GetComponent<RoomBehaviour>();
        fp = rbPrefab ? rbPrefab.gridSize : Vector2Int.one;
        if (fp.x < 1 || fp.y < 1) fp = Vector2Int.one;

        var world = new Vector3(x * cellSize.x, 0f, -y * cellSize.y);
        var go = Instantiate(def.prefab, world, Quaternion.identity, transform);
        go.name = $"{def.prefab.name} {x}-{y}";

        var netObj = go.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
            go.transform.SetParent(transform);
        }
        else 
        {
            go.transform.SetParent(transform);
            Debug.LogWarning($"[DungeonGenerator_MP] Room prefab {def.prefab.name} has no NetworkObject.");
        }

        var cell = board[x + y * size.x];
        cell.rb = go.GetComponent<RoomBehaviour>();
        cell.footprint = fp;
    }

    void ReserveFootprint(int x, int y, Vector2Int fp)
    {
        for (int dy = 0; dy < fp.y; dy++)
            for (int dx = 0; dx < fp.x; dx++)
                board[(x + dx) + (y + dy) * size.x].occupied = true;
    }

    void RemoveFootprintFromList(List<int> list, int x, int y, Vector2Int fp)
    {
        for (int dy = 0; dy < fp.y; dy++)
            for (int dx = 0; dx < fp.x; dx++)
            {
                int id = (x + dx) + (y + dy) * size.x;
                for (int i = 0; i < list.Count; i++)
                    if (list[i] == id) list[i] = -1;
            }
    }

    // ============================================================
    // Entrance pairing and visuals
    // ============================================================
    void FinalizeEntrances()
    {
        int W = size.x, H = size.y;

        // choose socket indices consistently along each shared edge
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                var a = board[x + y * W];
                if (a.rb == null) continue;

                // Up neighbor
                if (a.status[0] && y > 0)
                {
                    var b = board[x + (y - 1) * W];
                    if (b.rb != null && b.status[1])
                    {
                        PickSocketPair(a, 0, b, 1);
                    }
                }
                // Down neighbor
                if (a.status[1] && y < H - 1)
                {
                    var b = board[x + (y + 1) * W];
                    if (b.rb != null && b.status[0])
                    {
                        PickSocketPair(a, 1, b, 0);
                    }
                }
                // Right neighbor
                if (a.status[2] && x < W - 1)
                {
                    var b = board[(x + 1) + y * W];
                    if (b.rb != null && b.status[3])
                    {
                        PickSocketPair(a, 2, b, 3);
                    }
                }
                // Left neighbor
                if (a.status[3] && x > 0)
                {
                    var b = board[(x - 1) + y * W];
                    if (b.rb != null && b.status[2])
                    {
                        PickSocketPair(a, 3, b, 2);
                    }
                }
            }

        // apply to meshes
        for (int i = 0; i < board.Count; i++)
        {
            var c = board[i];
            if (c.rb == null) continue;

            bool[] open = new bool[4] { c.status[0], c.status[1], c.status[2], c.status[3] };
            int[] idxs = new int[4] { c.socketIndex[0], c.socketIndex[1], c.socketIndex[2], c.socketIndex[3] };

            c.rb.UpdateRoomWithIndices(open, idxs);
        }
    }

    void PickSocketPair(Cell a, int sideA, Cell b, int sideB)
    {
        // choose the same (clamped) index on both rooms
        int ca = a.rb.SocketCount(sideA);
        int cb = b.rb.SocketCount(sideB);
        if (ca <= 0 || cb <= 0) return;

        int idx = Mathf.Min(ca, cb) > 0 ? Random.Range(0, Mathf.Min(ca, cb)) : 0;
        a.socketIndex[sideA] = idx;
        b.socketIndex[sideB] = idx;
    }

    // ============================================================
    // Utils
    // ============================================================
    void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    void ClearChildren()
    {
        var toKill = new List<GameObject>();
        for (int i = transform.childCount - 1; i >= 0; i--)
            toKill.Add(transform.GetChild(i).gameObject);
        foreach (var go in toKill) DestroyImmediate(go);
    }
}
