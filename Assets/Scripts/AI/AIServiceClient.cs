using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AIServiceClient : MonoBehaviour
{
    [Header("Offline Mode")]
    public bool offlineOnly = false;

    [Serializable]
    private class BundleIn
    {
        public string subject;
        public string style;
        public string lore;
        public string size = "1024x1024";
        public bool force_regen = false;
    }

    public AIServiceConfig config;

    public IEnumerator GetBundle(string subject, string style, string lore,
        Action<BundleResponse> onDone, Action<string> onError,
        string size = "1024x1024", bool forceRegen = false)
    {
        if (offlineOnly)
        {
            onError?.Invoke("AIServiceClient.offlineOnly is true - not calling server");
            yield break;
        }

        var input = new BundleIn { subject = subject, style = style, lore = lore, size = size, force_regen = forceRegen };
        var json = JsonUtility.ToJson(input);

        using var req = new UnityWebRequest(config.baseUrl.TrimEnd('/') + "/bundle", "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        var body = req.downloadHandler != null ? req.downloadHandler.text : null;
        if (req.result != UnityWebRequest.Result.Success || req.responseCode >= 400)
        {
            onError?.Invoke($"HTTP {(long)req.responseCode}: {req.error}\n{body}");
            yield break;
        }

        var resp = JsonUtility.FromJson<BundleResponse>(body);
        onDone?.Invoke(resp);
    }

    public IEnumerator DownloadTexture(string pathOrUrl, Action<Texture2D> onDone, Action<string> onError)
    {
        if (offlineOnly)
        {
            onError?.Invoke("AIServiceClient.offlineOnly is true - not downloading texture");
            yield break;
        }

        var url = pathOrUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? pathOrUrl : config.baseUrl.TrimEnd('/') + pathOrUrl;

        using var req = UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(req.error);
            yield break;
        }

        var tex = DownloadHandlerTexture.GetContent(req);
        onDone?.Invoke(tex);
    }
}
