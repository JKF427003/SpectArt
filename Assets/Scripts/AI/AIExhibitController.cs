using System.Collections;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class AIExhibitController : MonoBehaviour, IRiddleInteractable
{
    public AIServiceClient service;
    public RiddleUIManager riddleUI;
    public ExhibitDataset dataset;
    public bool pickFromDatasetOnAwake = true;
    public bool onlyPickIfEmpty = true;

    [Header("Generation Settings")]
    public bool forceRegenOnStart = false;

    [Header("Offline / Cost Control")]
    [Tooltip("If ON, never call the service; only show items already cached.")]
    public bool offlineOnly = false; 

    [TextArea] public string subject = " ";
    [TextArea] public string style = " ";
    [TextArea] public string lore = " ";
    public string size = "1024x1024";

    MeshRenderer _mr;
    RiddleData _riddle;

    bool _started;
    bool _openWhenReady;

    void Awake() 
    { 
        _mr = GetComponent<MeshRenderer>();
        if (pickFromDatasetOnAwake && dataset != null)
        {
            bool subjectEmpty = string.IsNullOrWhiteSpace(subject);
            bool styleEmpty = string.IsNullOrWhiteSpace(style);
            bool loreEmpty = string.IsNullOrWhiteSpace(lore);

            if (!onlyPickIfEmpty || (subjectEmpty && styleEmpty && loreEmpty))
            {
                subject = dataset.PickSubject();
                style = dataset.PickStyle();
                lore = dataset.PickLore();

                Debug.Log($"[AIExhibitController] Picked prompts for {name}:\n" +
                          $"  Subject: {subject}\n  Style: {style}\n  Lore: {lore}");
            }
        }
    }

    void Start()
    {
        if (!service) service = FindFirstObjectByType<AIServiceClient>(FindObjectsInactive.Include);
        if (!riddleUI) riddleUI = FindFirstObjectByType<RiddleUIManager>(FindObjectsInactive.Include);
        TryStart();
    }

    void OnEnable() { TryStart(); }

    void TryStart()
    {
        if (_started) return;

        if (!offlineOnly && !service)
        {
            Debug.LogWarning("AIExhibitController: waiting for AIServiceClient (offlineOnly is OFF).");
            return;
        }
        if (!riddleUI)
        {
            Debug.LogWarning("AIExhibitController: waiting for RiddleUIManager...");
            return;
        }

        _started = true;
        StartCoroutine(GenerateAndShow());
    }

    IEnumerator GenerateAndShow()
    {
        string key = OfflineCache.KeyFrom(subject, style, lore, size);

        if (OfflineCache.TryLoad(key, out var cachedTex, out var cachedRiddle))
        {
            _riddle = cachedRiddle;
            ApplyTexture(cachedTex);
            Debug.Log($"[SpectArt] Loaded bundle from offline cache (key={key}).");

            if (_openWhenReady && _riddle != null)
            {
                _openWhenReady = false;
                riddleUI.Show(_riddle);
            }
            yield break; 
        }

        if (offlineOnly)
        {
            Debug.LogWarning($"[SpectArt] Cache miss for key={key}. offlineOnly is TRUE, skipping API. Trying any cached bundle instead.");

            if (OfflineCache.TryLoadAny(out var texAny, out var ridAny))
            {
                _riddle = ridAny;
                ApplyTexture(texAny);
                Debug.Log("[SpectArt] Loaded fallback bundle from offline cache (random).");

                if (_openWhenReady && _riddle != null)
                {
                    _openWhenReady = false;
                    riddleUI.Show(_riddle);
                }
            }
            else
            {
                Debug.LogError("[SpectArt] No cached bundles found at all – cannot show image offline.");
            }

            yield break;
        }

        bool liveOk = false;
        Debug.Log($"[SpectArt] Requesting LIVE bundle → subj='{subject}' | style='{style}' | lore='{lore}'");

        yield return service.GetBundle( subject, style, lore, (bundle) =>
        {

            _riddle = new RiddleData
            {
                Question = bundle.riddle.riddle,
                Correct = bundle.riddle.answer,
                Acceptable = (bundle.riddle.acceptable_answers != null && bundle.riddle.acceptable_answers.Length > 0) ? bundle.riddle.acceptable_answers : new[] { bundle.riddle.answer },
                Hint = bundle.riddle.hint
            };

            if (!string.IsNullOrEmpty(bundle.image_url))
            {
                StartCoroutine(service.DownloadTexture(
                    bundle.image_url,
                    (tex) =>
                    {
                        ApplyTexture(tex);
                        OfflineCache.Save(key, tex, _riddle);
                        Debug.Log("[SpectArt] Saved bundle to offline cache.");
                        if (_openWhenReady && _riddle != null)
                        {
                            _openWhenReady = false;
                            riddleUI.Show(_riddle);
                        }
                    },
                    (err) => Debug.LogWarning("Image download failed: " + err)
                ));
            }
            else
            {
                Debug.LogWarning("[SpectArt] No image_url returned; riddle only.");
                if (_openWhenReady && _riddle != null)
                {
                    _openWhenReady = false;
                    riddleUI.Show(_riddle);
                }
            }
            liveOk = true;
        },
        (err) => 
        {
            Debug.LogWarning("Live bundle failed; will not retry (offlineOnly is false). " + err);
        }, size, forceRegenOnStart);

        if (!liveOk && OfflineCache.TryLoad(key, out var tex2, out var rid2))
        {
            _riddle = rid2;
            ApplyTexture(tex2);
            Debug.Log("[SpectArt] Loaded bundle from cache after live fail.");
            if (_openWhenReady && _riddle != null)
            {
                _openWhenReady = false;
                riddleUI.Show(_riddle);
            }
        }
        else if (!liveOk)
        {
            Debug.LogError("[SpectArt] No internet and no cached bundle for this prompt.");
        }
    }

    void ApplyTexture(Texture2D tex)
    {
        if (!tex)
        {
            Debug.LogWarning($"[SpectArt] ApplyTexture called with null texture on {name}");
            return;
        }

        Debug.Log($"[SpectArt] ApplyTexture on {name}: {tex.width}x{tex.height}");

        var renderers = GetComponentsInChildren<MeshRenderer>();
        if (renderers == null || renderers.Length == 0)
        {
            Debug.LogWarning($"[SpectArt] No MeshRenderer found on {name} or its children.");
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

            if (mat.HasProperty("_MainTex_ST")) mat.SetTextureScale("_MainTex", Vector2.one);
            if (mat.HasProperty("_BaseMap_ST")) mat.SetTextureScale("_BaseMap", Vector2.one);
        }

        tex.wrapMode = TextureWrapMode.Clamp;
    }

    public void OpenRiddleUI()
    {
        // Make sure we have a valid reference
        if (!riddleUI)
            riddleUI = FindFirstObjectByType<RiddleUIManager>(FindObjectsInactive.Include);

        if (!riddleUI)
        {
            Debug.LogError("[SpectArt] OpenRiddleUI called but no RiddleUIManager found in the scene.");
            return;
        }

        // If the riddle is already ready, just show it
        if (_riddle != null)
        {
            Debug.Log("[SpectArt] Opening riddle UI with generated riddle.");
            riddleUI.Show(_riddle);
            return;
        }

        // Riddle not ready yet – mark that we want to open when it's ready
        _openWhenReady = true;

        // Kick generation if it hasn't started
        if (!_started)
            StartCoroutine(GenerateAndShow());

        // Show a temporary "loading" riddle so the player sees the UI immediately
        var placeholder = new RiddleData
        {
            Question = offlineOnly
                ? "Not cached for this frame (offline only)."
                : "Generating riddle…",
            Hint = "",
            Correct = "",
            Acceptable = new string[0]
        };

        Debug.Log("[SpectArt] Opening riddle UI with placeholder text while riddle generates.");
        riddleUI.Show(placeholder);
    }

    public void Interact()
    {
        Debug.Log($"[SpectArt] Interact on {name}");
        OpenRiddleUI();
    }
}