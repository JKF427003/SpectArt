using UnityEngine;

public class ArtFrameSinglePlayer : MonoBehaviour, IRiddleInteractable
{
    [Tooltip("Riddle assigned to this frame.")]
    public RiddleData riddle;

    public bool IsSolved { get; private set; }

    GameManagerSinglePlayer gameManager;

    void Awake()
    {
        // Register with GameManager when created (works for generated rooms too)
        if (GameManagerSinglePlayer.Instance != null)
        {
            GameManagerSinglePlayer.Instance.RegisterFrame(this);
        }
    }

    public void Register(GameManagerSinglePlayer gm)
    {
        gameManager = gm;
    }

    public void Interact()
    {
        if (IsSolved) return;

        RiddleUIManager.Instance.Show(riddle, this);
    }

    public void MarkSolved()
    {
        if (IsSolved) return;

        IsSolved = true;
        gameManager?.NotifyFrameSolved(this);
    }

    void OnDestroy()
    {
        if (GameManagerSinglePlayer.Instance != null)
        {
            GameManagerSinglePlayer.Instance.UnregisterFrame(this);
        }
    }
}
