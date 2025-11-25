using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RoomZone : MonoBehaviour
{
    public RoomMeta meta;

    void Reset()
    {
        if (!meta) meta = GetComponentInParent<RoomMeta>();
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        var tracker = other.GetComponent<PlayerRoomTracker>();
        if (tracker && meta)
            tracker.NotifyEnterRoom(meta);
    }

    void OnTriggerExit(Collider other)
    {
        var tracker = other.GetComponent<PlayerRoomTracker>();
        if (tracker && meta)
            tracker.NotifyExitRoom(meta);
    }
}
