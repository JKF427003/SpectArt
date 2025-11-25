using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class SimpleInteractor_IS : MonoBehaviour
{
    public float interactRange = 3f;
    public LayerMask interactMask = ~0;
    public bool debug = false;

    [SerializeField] Camera cam;
    [SerializeField] RiddleUIManager riddleUI;

    [Header("Animation")]
    [Tooltip("Animator on the Character (same one used by locomotion).")]
    [SerializeField] Animator animator;
    [Tooltip("Trigger name for the reach/interaction animation in the UpperBody layer.")]
    [SerializeField] string interactTrigger = "Interact";
    [Tooltip("Delay before actually invoking Interact() to sync with the reach pose.")]
    [SerializeField] float interactAnimDelay = 0.20f;

    private void Awake()
    {
        if (!cam) cam = GetComponent<Camera>();
        if (!cam) cam = Camera.main;
        if (!riddleUI) riddleUI = FindFirstObjectByType<RiddleUIManager>(FindObjectsInactive.Include);
        if (!animator) animator = FindFirstObjectByType<Animator>();
    }
    void Update()
    {
        if (riddleUI && riddleUI.IsOpen) return;
        if (!cam || Keyboard.current == null) return;

        if (debug)
            Debug.DrawRay(cam.transform.position, cam.transform.forward * interactRange, Color.cyan);

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            Debug.Log("[Interactor] E pressed");

            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, interactRange, interactMask))
            {
                Debug.Log($"[Interactor] Hit: {hit.collider.name}");

                var target = hit.collider.GetComponent<IRiddleInteractable>() ??
                             hit.collider.GetComponentInParent<IRiddleInteractable>() ??
                             hit.collider.GetComponentInChildren<IRiddleInteractable>();

                if (target == null)
                {
                    Debug.Log("[Interactor] No IRiddleInteractable on hit object.");
                    return;
                }

                Debug.Log($"[Interactor] Found IRiddleInteractable on {((Component)target).gameObject.name}");

                if (animator) animator.SetTrigger(interactTrigger);
                StartCoroutine(CommitAfterDelay(target, interactAnimDelay));
            }
            else
            {
                Debug.Log("[Interactor] Raycast hit nothing.");
            }
        }
    }

    IEnumerator CommitAfterDelay(IRiddleInteractable target, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        target.Interact();
    }
}