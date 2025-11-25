using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject multiplayerPanel;

    [Header("Multiplayer UI")]
    [SerializeField] private TMP_InputField joinRoomCodeInput; // "Enter Room Code..."
    [SerializeField] private TMP_Text hostRoomCodeLabel;       // text next to "Room Code:"
    [SerializeField] private Button startGameButton;           // new "Start Game" button

    [Header("Scene Names")]
    [SerializeField] private string singlePlayerScene = "GameScene";
    [SerializeField] private string multiplayerScene = "GameTest_Multiplayer";

    private string currentRoomCode;

    private void Awake()
    {
        if (multiplayerPanel != null) multiplayerPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (startGameButton != null) startGameButton.interactable = false;

        var nm = NetworkManager.Singleton;
        if (nm != null)
        {
            DontDestroyOnLoad(nm.gameObject);
        }
    }

    // ========== MAIN MENU BUTTONS ==========

    public void OnSingleplayerClicked()
    {
        SceneManager.LoadScene(singlePlayerScene);
    }

    public void OnMultiplayerClicked()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (multiplayerPanel) multiplayerPanel.SetActive(true);
    }

    public void OnCloseMultiplayerClicked()
    {
        if (multiplayerPanel) multiplayerPanel.SetActive(false);
        if (mainMenuPanel) mainMenuPanel.SetActive(true);
    }

    public void OnExitGameClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        Debug.Log("[MainMenuUI] Quit Game (editor only).");
#endif
    }

    // ========== MULTIPLAYER FLOW ==========

    // "Create Room"
    public void OnHostPressed()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[MainMenuUI] No NetworkManager found in scene.");
            return;
        }

        if (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
        {
            bool ok = NetworkManager.Singleton.StartHost();
            if (!ok)
            {
                Debug.LogError("[MainMenuUI] StartHost failed.");
                return;
            }
        }

        currentRoomCode = GenerateRoomCode(6);

        if (hostRoomCodeLabel != null)
            hostRoomCodeLabel.text = $"{currentRoomCode}";

        if (startGameButton != null)
            startGameButton.interactable = true;

        Debug.Log($"[MainMenuUI] Hosting session with room code {currentRoomCode}");
    }

    // "Start Game"
    public void OnStartGamePressed()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[MainMenuUI] No NetworkManager found.");
            return;
        }

        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning("[MainMenuUI] Only the host can start the game.");
            return;
        }

        // Use Netcode scene manager so ALL clients move to the game scene
        NetworkManager.Singleton.SceneManager.LoadScene(multiplayerScene, LoadSceneMode.Single);
    }

    // "Join Room"
    public void OnJoinPressed()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[MainMenuUI] No NetworkManager found in scene.");
            return;
        }

        string code = joinRoomCodeInput != null ? joinRoomCodeInput.text.Trim() : string.Empty;

        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("[MainMenuUI] Join pressed with empty room code.");
            return;
        }

        // For now the code is cosmetic – connection still goes to the configured transport.
        Debug.Log($"[MainMenuUI] Joining room with code {code}");

        bool ok = NetworkManager.Singleton.StartClient();
        if (!ok)
        {
            Debug.LogError("[MainMenuUI] StartClient failed.");
        }

        // No scene load here – client waits for host to use SceneManager.LoadScene
    }

    private string GenerateRoomCode(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        char[] codeChars = new char[length];
        for (int i = 0; i < length; i++)
            codeChars[i] = chars[Random.Range(0, chars.Length)];

        return new string(codeChars);
    }
}