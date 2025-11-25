using UnityEngine;
using UnityEngine.AI;

// If you use the New Input System, add this define via package; it's auto-defined by Unity.
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DefaultExecutionOrder(50)]
public class EnemyAnimationDirector : MonoBehaviour
{
    [Header("Scene refs")]
    public Animator animator;
    public NavMeshAgent agent;
    public Transform lookTargetOverride;

    [Header("Test Controls (no AI yet)")]
    public bool useAgentSpeed = true;
    [Range(0f, 5f)] public float testSpeed = 0f;
    public bool isStaring = false;
    public bool isChasing = false;
    [Range(0f, 1f)] public float crouch = 0f;

    [Header("Head Look (only while staring)")]
    public bool enableHeadLook = true;
    [Range(0f, 120f)] public float maxYaw = 80f;
    [Range(0f, 80f)] public float maxPitch = 45f;
    [Range(1f, 20f)] public float headTurnSpeed = 10f;

    Transform headBone;
    Quaternion headDefaultLocal;

    void Reset()
    {
        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (animator && animator.isHuman)
        {
            headBone = animator.GetBoneTransform(HumanBodyBones.Head);
            if (headBone) headDefaultLocal = headBone.localRotation;
        }
    }

    void Update()
    {
        HandleDebugInput(); // safe on either input backend

        float speed = useAgentSpeed && agent ? agent.velocity.magnitude : testSpeed;
        animator?.SetFloat("Speed", speed);
        animator?.SetBool("IsStaring", isStaring);
        animator?.SetBool("IsChasing", isChasing);
        animator?.SetFloat("Crouch", crouch);
    }

    void LateUpdate()
    {
        if (!enableHeadLook || !isStaring || headBone == null) return;
        Transform target = lookTargetOverride ? lookTargetOverride : (Camera.main ? Camera.main.transform : null);
        if (!target) return;

        Vector3 to = target.position - headBone.position;
        if (to.sqrMagnitude < 0.0001f) return;

        Vector3 localDir = headBone.parent.InverseTransformDirection(to.normalized);
        float yaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
        float pitch = -Mathf.Asin(Mathf.Clamp(localDir.y, -1f, 1f)) * Mathf.Rad2Deg;

        yaw = Mathf.Clamp(yaw, -maxYaw, maxYaw);
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

        Quaternion targetLocal =
            Quaternion.Euler(pitch, 0f, 0f) *
            Quaternion.Euler(0f, yaw, 0f) *
            headDefaultLocal;

        headBone.localRotation = Quaternion.Slerp(headBone.localRotation, targetLocal, Time.deltaTime * headTurnSpeed);
    }

    void OnDisable() => ResetHeadPose();
    public void ResetHeadPose() { if (headBone) headBone.localRotation = headDefaultLocal; }

    // --- Input that works with either system ---
    void HandleDebugInput()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        // New Input System
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) { isStaring = false; isChasing = false; }
            if (Keyboard.current.digit2Key.wasPressedThisFrame) { isStaring = true; isChasing = false; crouch = 0f; }
            if (Keyboard.current.digit3Key.wasPressedThisFrame) { isStaring = true; isChasing = false; crouch = 1f; }
            if (Keyboard.current.digit4Key.wasPressedThisFrame) { isStaring = false; isChasing = true; }
        }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            animator?.SetTrigger("Attack");
#else
        // Old Input Manager
        if (Input.GetKeyDown(KeyCode.Alpha1)) { isStaring = false; isChasing = false; }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { isStaring = true;  isChasing = false; crouch = 0f; }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { isStaring = true;  isChasing = false; crouch = 1f; }
        if (Input.GetKeyDown(KeyCode.Alpha4)) { isStaring = false; isChasing = true; }
        if (Input.GetMouseButtonDown(0))      animator?.SetTrigger("Attack");
#endif
    }
}
