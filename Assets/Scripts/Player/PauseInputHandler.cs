using UnityEngine;
using UnityEngine.InputSystem;

public class PauseInputHandler : MonoBehaviour
{
    [Tooltip("Reference to the Pause action from your Input Actions asset.")]
    public InputActionReference pauseAction;

    void OnEnable()
    {
        if (pauseAction != null)
            pauseAction.action.performed += OnPausePerformed;
    }

    void OnDisable()
    {
        if (pauseAction != null)
            pauseAction.action.performed -= OnPausePerformed;
    }

    void OnPausePerformed(InputAction.CallbackContext ctx)
    {
        if (GameManagerSinglePlayer.Instance != null)
        {
            GameManagerSinglePlayer.Instance.TogglePause();
        }
    }
}