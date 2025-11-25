#if UNITY_EDITOR
using System;
using System.Net.Sockets;
using System.Threading;
using UnityEditor;
using UnityEngine;

using Proc = System.Diagnostics.Process;
using PSI = System.Diagnostics.ProcessStartInfo;

[InitializeOnLoad]
public static class AutoServiceStarter
{
    static Proc proc;
    static ServiceLauncherConfig cfg;

    static AutoServiceStarter()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        cfg = AssetDatabase.LoadAssetAtPath<ServiceLauncherConfig>("Assets/Data/ServiceLauncherConfig.asset");
        if (cfg == null)
            UnityEngine.Debug.LogWarning("AutoServiceStarter: ServiceLauncherConfig.asset not found at Assets/SpectArt/");
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (cfg == null) return;

        if (state == PlayModeStateChange.ExitingEditMode)
        {
            if (!IsPortOpen("127.0.0.1", cfg.port))
            {
                try
                {
                    var psi = new PSI
                    {
                        FileName = Combine(cfg.workingDirectory, cfg.pythonExe),
                        Arguments = "-m uvicorn " + cfg.uvicornArgs,
                        WorkingDirectory = cfg.workingDirectory,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    proc = Proc.Start(psi);

                    var t0 = DateTime.UtcNow;
                    while (!IsPortOpen("127.0.0.1", cfg.port) &&
                           (DateTime.UtcNow - t0).TotalMilliseconds < cfg.startupTimeoutMs)
                    {
                        Thread.Sleep(200);
                    }

                    UnityEngine.Debug.Log($"FastAPI server started (port {cfg.port}).");
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError("Failed to start FastAPI: " + e);
                }
            }
            else
            {
                UnityEngine.Debug.Log($"ℹ️ FastAPI already running on port {cfg.port}.");
            }
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            TryKill();
        }
    }

    static string Combine(string dir, string relOrAbs)
        => System.IO.Path.IsPathRooted(relOrAbs)
           ? relOrAbs
           : System.IO.Path.Combine(dir, relOrAbs);

    static bool IsPortOpen(string host, int port)
    {
        try
        {
            using var c = new TcpClient();
            var ar = c.BeginConnect(host, port, null, null);
            ar.AsyncWaitHandle.WaitOne(200);
            return c.Connected;
        }
        catch { return false; }
    }

    static void TryKill()
    {
        if (proc != null && !proc.HasExited)
        {
            try
            {
                proc.Kill();              
                proc.WaitForExit(2000);   
            }
            catch { /* swallow */ }
        }
        proc = null;
    }
}
#endif
