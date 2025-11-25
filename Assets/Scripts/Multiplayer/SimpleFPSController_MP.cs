using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class SimpleFPSController_MP : NetworkBehaviour
{
    public static bool InputLocked = false;

    [Header("Camera & Movement")]
    public Camera playerCamera;
    public Transform cameraPivot;
    public float walkSpeed = 4.5f;
    public float sprintSpeed = 7.5f;
    public float jumpHeight = 1.2f;
    public float gravity = -9.81f;
    public float mouseSensitivity = 0.15f;
    [Range(-89f, 0f)] public float minPitch = -60f;
    [Range(0f, 89f)] public float maxPitch = 80f;

    [Header("Animation")]
    public Animator animator;
    public string speedParam = "Speed";
    public string groundedParam = "IsGrounded";
    public string jumpTrigger = "Jump";

    CharacterController _cc;
    float _yVel, _pitch;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!playerCamera) playerCamera = GetComponentInChildren<Camera>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!cameraPivot && playerCamera) cameraPivot = playerCamera.transform.parent;

        if (IsOwner)
        {
            if (playerCamera) playerCamera.enabled = true;
            var al = playerCamera ? playerCamera.GetComponent<AudioListener>() : null;
            if (al) al.enabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            if (playerCamera) playerCamera.enabled = false;
            var al = playerCamera ? playerCamera.GetComponent<AudioListener>() : null;
            if (al) al.enabled = false;
        }
    }

    void Update()
    {
        if (!IsOwner) return;
        if (InputLocked) return;

        Look();
        Move();

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void Look()
    {
        if (Mouse.current == null || playerCamera == null) return;

        var md = Mouse.current.delta.ReadValue();

        transform.Rotate(Vector3.up * (md.x * mouseSensitivity));
        _pitch = Mathf.Clamp(_pitch - md.y * mouseSensitivity, minPitch, maxPitch);

        if (cameraPivot)
            cameraPivot.localEulerAngles = new Vector3(_pitch, 0f, 0f);
        else
            playerCamera.transform.localEulerAngles = new Vector3(_pitch, 0f, 0f);
    }

    void Move()
    {
        bool grounded = _cc.isGrounded;
        if (grounded && _yVel < -2f) _yVel = -2f;

        float spd = Keyboard.current.leftShiftKey.isPressed ? sprintSpeed : walkSpeed;

        float x = 0f, z = 0f;
        if (Keyboard.current.aKey.isPressed) x -= 1f;
        if (Keyboard.current.dKey.isPressed) x += 1f;
        if (Keyboard.current.sKey.isPressed) z -= 1f;
        if (Keyboard.current.wKey.isPressed) z += 1f;

        var input = new Vector3(x, 0f, z).normalized;
        var moveHoriz = (transform.right * input.x + transform.forward * input.z) * spd;

        /*if (grounded && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            _yVel = Mathf.Sqrt(jumpHeight * -2f * gravity);
            if (animator) animator.SetTrigger(jumpTrigger);
        }*/

        _yVel += gravity * Time.deltaTime;

        var move = moveHoriz;
        move.y = _yVel;
        _cc.Move(move * Time.deltaTime);

        if (animator)
        {
            animator.SetFloat(speedParam, moveHoriz.magnitude, 0.1f, Time.deltaTime);
            animator.SetBool(groundedParam, grounded);
        }
    }
}