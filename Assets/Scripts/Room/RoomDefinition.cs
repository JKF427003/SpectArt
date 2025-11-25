using UnityEngine;

[System.Serializable]
public class RoomDefinition
{
    public GameObject prefab;
    public int requiredCount; // -1 for unlimited/random
    public Vector2 roomSize;  // width, height
    public Vector2 offset;    // spacing offsets
}
