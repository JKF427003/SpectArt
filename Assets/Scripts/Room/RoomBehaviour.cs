using System.Collections.Generic;
using UnityEngine;

public class RoomBehaviour : MonoBehaviour
{
    [System.Serializable]
    public class Socket
    {
        [Tooltip("Wall mesh (shown when CLOSED)")]
        public GameObject wall;
        [Tooltip("Door/arch mesh (shown when OPEN)")]
        public GameObject door;

        public void SetOpen(bool open)
        {
            if (door) door.SetActive(open);
            if (wall) wall.SetActive(!open);
        }
    }

    [System.Serializable]
    public class Side
    {
        public List<Socket> sockets = new List<Socket>();

        public void CloseAll()
        {
            foreach (var s in sockets) s.SetOpen(false);
        }

        public void OpenIndex(int index)
        {
            if (sockets == null || sockets.Count == 0) return;
            index = Mathf.Clamp(index, 0, sockets.Count - 1);
            for (int i = 0; i < sockets.Count; i++)
                sockets[i].SetOpen(i == index);
        }
    }

    [Header("How many grid cells this prefab occupies")]
    public Vector2Int gridSize = new Vector2Int(1, 1);

    [Header("Define available entrances (leave a side empty if unsupported)")]
    public Side up = new Side();    // +Z (top in Scene view if Z points up)
    public Side down = new Side();  // -Z
    public Side right = new Side(); // +X
    public Side left = new Side();  // -X

    public int SocketCount(int side)
    {
        switch (side)
        {
            case 0: return up?.sockets?.Count ?? 0;
            case 1: return down?.sockets?.Count ?? 0;
            case 2: return right?.sockets?.Count ?? 0;
            case 3: return left?.sockets?.Count ?? 0;
            default: return 0;
        }
    }

    /// Opens exactly ONE socket per open side. Closed sides show walls.
    public void UpdateRoomWithIndices(bool[] open4, int[] index4)
    {
        for (int side = 0; side < 4; side++)
        {
            var s = side == 0 ? up : side == 1 ? down : side == 2 ? right : left;
            if (open4 != null && side < open4.Length && open4[side] && SocketCount(side) > 0)
                s.OpenIndex(index4 != null && side < index4.Length ? index4[side] : 0);
            else
                s.CloseAll();
        }
    }

    // Back-compat (always index 0 when open)
    public void UpdateRoom(bool[] open4) =>
        UpdateRoomWithIndices(open4, new[] { 0, 0, 0, 0 });
}
