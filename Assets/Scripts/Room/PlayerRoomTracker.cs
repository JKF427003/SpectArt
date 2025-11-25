using UnityEngine;

public class PlayerRoomTracker : MonoBehaviour
{
    public RoomMeta CurrentRoom { get; private set; }

    public void NotifyEnterRoom(RoomMeta room)
    {
        CurrentRoom = room;
        EnemyDirector.Instance?.OnPlayerEnteredRoom(room);
    }

    public void NotifyExitRoom(RoomMeta room)
    {
        if (CurrentRoom == room)
            CurrentRoom = null;

        EnemyDirector.Instance?.OnPlayerExitedRoom(room);
    }
}
