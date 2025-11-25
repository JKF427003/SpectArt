using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class RiddleUIManager : MonoBehaviour
{
    public static RiddleUIManager Instance { get; private set; }

    [Header("Root Panel")]
    public GameObject panel;

    [Header("Texts")]
    public TMP_Text riddleText;
    public TMP_Text hintText;

    [Header("Input")]
    public TMP_InputField answerInput;
    public Button submitButton;
    public Button hintButton;
    public Button closeButton;

    public bool IsOpen => panel && panel.activeSelf;

    RiddleData currentRiddle;
    ArtFrameSinglePlayer currentFrame;
    int hintStage = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (panel != null) panel.SetActive(false);

        if (submitButton) submitButton.onClick.AddListener(OnSubmit);
        if (hintButton) hintButton.onClick.AddListener(OnHint);
        if (closeButton) closeButton.onClick.AddListener(Hide);
    }

    public void Show(RiddleData data)
    {
        Show(data, null);
    }

    public void Show(RiddleData data, ArtFrameSinglePlayer frame)
    {
        currentRiddle = data;
        currentFrame = frame;
        hintStage = 0;

        if (!panel)
        {
            Debug.LogWarning("[RiddleUI] No panel set.");
            return;
        }

        panel.SetActive(true);

        if (riddleText) riddleText.text = data?.Question ?? "(no question)";
        if (hintText) hintText.text = "";

        if (answerInput)
        {
            answerInput.text = "";
            answerInput.ActivateInputField();
        }

        Debug.Log("[RiddleUI] Show: Panel should now be visible.");

        SimpleFPSController_MP.InputLocked = true;
        SimpleFPSController_IS.InputLocked = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Hide()
    {
        if (!panel) return;

        panel.SetActive(false);

        SimpleFPSController_MP.InputLocked = false;
        SimpleFPSController_IS.InputLocked = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        currentFrame = null;
        currentRiddle = null;
    }

    void OnSubmit()
    {
        if (currentRiddle == null || answerInput == null) return;
        var ans = answerInput.text.Trim().ToLowerInvariant();

        if (ans == currentRiddle.Correct.ToLowerInvariant() || (currentRiddle.Acceptable != null && System.Array.Exists(currentRiddle.Acceptable, a => a.ToLowerInvariant() == ans)))
        {
            Debug.Log("[RiddleUI] Correct!");
            currentFrame?.MarkSolved();
            Hide();
        }
        else
        {
            Debug.Log("[RiddleUI] Incorrect. Try again.");
        }
    }

    void OnHint()
    {
        if (currentRiddle == null || hintText == null) return;
        hintStage++;
        hintText.gameObject.SetActive(true);

        switch (hintStage)
        {
            case 1: hintText.text = currentRiddle.Hint; break;
            case 2: hintText.text = "Starts with: " + currentRiddle.Correct[0]; break;
            default: hintText.text = $"Answer length: {currentRiddle.Correct.Length} letters"; break;
        }
    }
}
