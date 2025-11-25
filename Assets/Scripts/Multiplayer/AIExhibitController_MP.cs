using System.Collections;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class AIExhibitController_MP : MonoBehaviour, IRiddleInteractable
{
    [Header("Dependencies")]
    public RiddleUIManager riddleUI;
    public ExhibitDataset dataset;

    [Header("Prompt Settings")]
    public bool pickFromDatasetOnAwake = true;
    public bool onlyPickIfEmpty = true;

    [TextArea] public string subject = "";
    [TextArea] public string style = "";
    [TextArea] public string lore = "";
    public string size = "1024x1024";

    [Header("Offline Control")]
    [Tooltip("If ON, never call the online service; only use OfflineCache.")]
    public bool offlineOnly = true;

    MeshRenderer _mr;
    RiddleData _riddle;
    bool _started;
    bool _openWhenReady;

    void Awake()
    {
        _mr = GetComponent<MeshRenderer>();

        // Pick prompts locally (no networking) if desired
        if (pickFromDatasetOnAwake && dataset != null)
        {
            bool subjEmpty = string.IsNullOrWhiteSpace(subject);
            bool styleEmpty = string.IsNullOrWhiteSpace(style);
            bool loreEmpty = string.IsNullOrWhiteSpace(lore);

            if (!onlyPickIfEmpty || (subjEmpty && styleEmpty && loreEmpty))
            {
                subject = dataset.PickSubject();
                style = dataset.PickStyle();
                lore = dataset.PickLore();
            }
        }
    }

    void Start()
    {
        TryStart();
    }

    void OnEnable()
    {
        TryStart();
    }

    void TryStart()
    {
        if (_started) return;
        if (!riddleUI)
        {
            riddleUI = FindFirstObjectByType<RiddleUIManager>(FindObjectsInactive.Include);
            if (!riddleUI)
            {
                Debug.LogWarning("[AIExhibitController_MP] Waiting for RiddleUIManager…");
                return;
            }
        }

        _started = true;
        StartCoroutine(LoadFromCacheOffline());
    }

    IEnumerator LoadFromCacheOffline()
    {
        // Always offline: only touch OfflineCache
        string key = OfflineCache.KeyFrom(subject, style, lore, size);
        Debug.Log($"[AIExhibitController_MP] Offline load for '{name}' key={key}");

        // 1) Try exact key
        if (OfflineCache.TryLoad(key, out var tex, out var rid))
        {
            _riddle = rid;
            ApplyTexture(tex);
            if (_openWhenReady && _riddle != null)
            {
                _openWhenReady = false;
                riddleUI.Show(_riddle);
            }
            yield break;
        }

        // 2) Fallback: any cached bundle
        if (OfflineCache.TryLoadAny(out var texAny, out var ridAny))
        {
            _riddle = ridAny;
            ApplyTexture(texAny);
            Debug.Log("[AIExhibitController_MP] Loaded fallback bundle from offline cache (random).");

            if (_openWhenReady && _riddle != null)
            {
                _openWhenReady = false;
                riddleUI.Show(_riddle);
            }
        }
        else
        {
            Debug.LogError("[AIExhibitController_MP] No cached bundles found at all – cannot show image offline.");
        }
    }

    void ApplyTexture(Texture2D tex)
    {
        if (!tex)
        {
            Debug.LogWarning($"[AIExhibitController_MP] ApplyTexture called with null texture on {name}");
            return;
        }

        var renderers = GetComponentsInChildren<MeshRenderer>();
        if (renderers == null || renderers.Length == 0)
        {
            Debug.LogWarning($"[AIExhibitController_MP] No MeshRenderer found on {name} or its children.");
            return;
        }

        foreach (var mr in renderers)
        {
            var mat = mr.material;
            var unlit = Shader.Find("Unlit/Texture");
            if (unlit) mat.shader = unlit;

            mat.mainTexture = tex;

            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);

            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.white * 0.2f);
                if (mat.HasProperty("_EmissionMap")) mat.SetTexture("_EmissionMap", tex);
            }
        }

        tex.wrapMode = TextureWrapMode.Clamp;
    }

    public void OpenRiddleUI()
    {
        if (!riddleUI)
            riddleUI = FindFirstObjectByType<RiddleUIManager>(FindObjectsInactive.Include);

        if (!riddleUI)
        {
            Debug.LogError("[AIExhibitController_MP] OpenRiddleUI called but no RiddleUIManager found.");
            return;
        }

        if (_riddle != null)
        {
            riddleUI.Show(_riddle);
            return;
        }

        _openWhenReady = true;

        var placeholder = new RiddleData
        {
            Question = "Loading cached riddle…",
            Hint = "",
            Correct = "",
            Acceptable = new string[0]
        };

        riddleUI.Show(placeholder);
    }

    public void Interact()
    {
        OpenRiddleUI();
    }
}