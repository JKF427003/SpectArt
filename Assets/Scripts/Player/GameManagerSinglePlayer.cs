using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManagerSinglePlayer : MonoBehaviour
{
    public static GameManagerSinglePlayer Instance { get; private set; }

    [Header("Win condition")]
    public int requiredFramesToWin = 0;

    [Header("UI")]
    public TMP_Text framesLeftText;
    public GameObject pauseMenuRoot;
    public string startSceneName = "StartScene";

    [Header("Scripts to disable while paused")]
    public MonoBehaviour[] scriptsToDisableWhenPaused;   // <— assign in Inspector

    readonly List<ArtFrameSinglePlayer> frames = new List<ArtFrameSinglePlayer>();
    int solvedFrames;
    bool isPaused;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Time.timeScale = 1f;
        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);
    }

    void Start()
    {
        // In case some frames were already registered in Awake
        RecalculateTotals();
        UpdateFramesUI();
    }

    // ---------- FRAME REGISTRATION ----------

    public void RegisterFrame(ArtFrameSinglePlayer frame)
    {
        if (!frames.Contains(frame))
        {
            frames.Add(frame);
            frame.Register(this);
            RecalculateTotals();
            UpdateFramesUI();
        }
    }

    public void UnregisterFrame(ArtFrameSinglePlayer frame)
    {
        if (frames.Remove(frame))
        {
            RecalculateTotals();
            UpdateFramesUI();
        }
    }

    void RecalculateTotals()
    {
        int totalFrames = frames.Count;

        if (requiredFramesToWin <= 0 || requiredFramesToWin > totalFrames)
            requiredFramesToWin = totalFrames;

        if (solvedFrames > requiredFramesToWin)
            solvedFrames = requiredFramesToWin;
    }

    public void NotifyFrameSolved(ArtFrameSinglePlayer frame)
    {
        // Re-count solved frames, in case they’re generated dynamically
        solvedFrames = 0;
        foreach (var f in frames)
            if (f.IsSolved)
                solvedFrames++;

        UpdateFramesUI();

        if (requiredFramesToWin > 0 && solvedFrames >= requiredFramesToWin)
            HandleWin();
    }

    void UpdateFramesUI()
    {
        if (framesLeftText == null) return;

        int left = Mathf.Max(0, requiredFramesToWin - solvedFrames);
        framesLeftText.text = $"Frames left: {left}/{requiredFramesToWin}";
    }

    // ---------- WIN / LOSE ----------

    void HandleWin()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(startSceneName);
    }

    public void HandlePlayerCaught()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(startSceneName);
    }

    // ---------- PAUSE ----------

    public void TogglePause()
    {
        if (isPaused) ResumeGame();
        else PauseGame();
    }

    public void PauseGame()
    {
        if (isPaused) return;
        isPaused = true;

        Time.timeScale = 0f;

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(true);

        // Disable movement / look scripts
        foreach (var s in scriptsToDisableWhenPaused)
            if (s != null)
                s.enabled = false;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ResumeGame()
    {
        if (!isPaused) return;
        isPaused = false;

        Time.timeScale = 1f;

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);

        // Re-enable movement / look scripts
        foreach (var s in scriptsToDisableWhenPaused)
            if (s != null)
                s.enabled = true;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(startSceneName);
    }

    public bool IsPaused => isPaused;
}