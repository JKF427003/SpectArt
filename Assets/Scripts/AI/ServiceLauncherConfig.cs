using UnityEngine;

[CreateAssetMenu(fileName = "ServiceLauncherConfig", menuName = "Scriptable Objects/ServiceLauncherConfig")]
public class ServiceLauncherConfig : ScriptableObject
{
    [Header("Local FastAPI server")]
    [Tooltip("Working dir of your Python service project (contains main.py)")]
    public string workingDirectory = @"C:\Users\jkfar\Documents\GitHub\future-game-tech-s25-l-indannati\spectart-ai-service";

    [Tooltip("Path to python.exe (absolute, or relative to workingDirectory)")]
    public string pythonExe = @".venv\Scripts\python.exe";

    [Tooltip("Args to launch uvicorn")]
    public string uvicornArgs = "main:app --port 8000";

    [Tooltip("Port to wait for before letting Unity start")]
    public int port = 8000;

    [Tooltip("Max ms to wait for startup before giving up")]
    public int startupTimeoutMs = 15000;
}

