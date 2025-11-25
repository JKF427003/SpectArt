using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ArtFrameSnapToWall : MonoBehaviour
{
    [Tooltip("How far back to search for a wall behind the frame.")]
    public float maxDistance = 1.0f;

    [Tooltip("Small offset so the frame is not z-fighting with the wall.")]
    public float offset = 0.01f;

    [Tooltip("Which layers count as walls.")]
    public LayerMask wallMask = ~0;

    void Start()
    {
        Vector3 origin = transform.position + transform.forward * 0.5f;
        Vector3 dir = -transform.forward;

        if (Physics.Raycast(origin, dir, out var hit, maxDistance, wallMask))
        {
            transform.position = hit.point + hit.normal * offset;
            transform.rotation = Quaternion.LookRotation(-hit.normal, Vector3.up);
        }
        else
        {
            // Debug.LogWarning($"[SpectArt] No wall found behind {name} to snap to.");
        }
    }
}
