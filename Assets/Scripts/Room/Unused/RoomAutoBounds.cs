using UnityEngine;

[DisallowMultipleComponent]
public class RoomAutoBounds : MonoBehaviour
{
    public bool addIfMissing = true;

    void Start()
    {
        if (!addIfMissing) return;
        if (!GetComponent<RoomZone>()) gameObject.AddComponent<RoomZone>();
        var col = GetComponent<BoxCollider>();
        if (!col) col = gameObject.AddComponent<BoxCollider>();
        col.isTrigger = true;

        var rends = GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return;

        var bounds = new Bounds(transform.position, Vector3.zero);
        foreach (var r in rends) bounds.Encapsulate(r.bounds);

        // Convert world bounds to local box collider values
        col.center = transform.InverseTransformPoint(bounds.center);
        var extentsWorld = bounds.size * 0.5f;
        // Approximate local size (assumes uniform/non-sheared scale)
        var inv = transform.worldToLocalMatrix;
        var localA = inv.MultiplyVector(new Vector3(extentsWorld.x, 0, 0)).magnitude;
        var localB = inv.MultiplyVector(new Vector3(0, extentsWorld.y, 0)).magnitude;
        var localC = inv.MultiplyVector(new Vector3(0, 0, extentsWorld.z)).magnitude;
        col.size = new Vector3(localA * 2, localB * 2, localC * 2);
    }
}
