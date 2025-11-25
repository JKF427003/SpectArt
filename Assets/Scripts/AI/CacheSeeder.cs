using System.Collections;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;

public class CacheSeeder : MonoBehaviour
{
    public AIServiceClient service;
    public ExhibitDataset dataset;

    [Header("Generation Settings")]
    public int count = 10;
    public string size = "1024x1024";
    public bool forceRegen = false;

    [Header("Cost Control")]
    [Tooltip("Only seed when cache has fewer than MinCachedEntries PNG files.")]
    public bool onlySeedWhenBelowThreshold = true;

    [Tooltip("Minimum number of cached entries before we stop seeding.")]
    public int minCachedEntries = 40;

    [Tooltip("Relative cache folder (must match OfflineCache).")]
    public string cacheFolderName = "ExhibitCache";

    private void Start()
    {
        if (!service) service = FindFirstObjectByType<AIServiceClient>(FindObjectsInactive.Include);
        if (!dataset) dataset = FindFirstObjectByType<ExhibitDataset>();

        if (!service || !dataset)
        {
            Debug.LogError("[Seeder] Missing service or dataset.");
            return;
        }

        if (onlySeedWhenBelowThreshold && HasEnoughCachedContent())
        {
            Debug.Log("[Seeder] Cache already above threshold, skipping seeding.");
            return;
        }

        StartCoroutine(Seed());
    }

    private bool HasEnoughCachedContent()
    {
        string root = Path.Combine(Application.persistentDataPath, cacheFolderName);
        if (!Directory.Exists(root))
            return false;

        int pngCount = Directory.GetFiles(root, "*.png", SearchOption.AllDirectories).Length;
        Debug.Log($"[Seeder] Found {pngCount} cached PNGs.");
        return pngCount >= minCachedEntries;
    }

    IEnumerator Seed()
    {
        for (int i = 0; i < count; i++)
        {
            string subject = dataset.PickSubject();
            string style = dataset.PickStyle();
            string lore = dataset.PickLore();

            string key = OfflineCache.KeyFrom(subject, style, lore, size);

            if (!forceRegen && OfflineCache.TryLoad(key, out _, out _))
            {
                Debug.Log($"[Seeder] Already cached > {key}, skipping.");
                continue;
            }

            Debug.Log($"[Seeder] Requesting new bundle #{i + 1}/{count} → {subject} | {style} | {lore}");

            bool done = false;

            yield return service.GetBundle(
                subject, style, lore,
                (bundle) =>
                {
                    var riddle = new RiddleData
                    {
                        Question = bundle.riddle.riddle,
                        Correct = bundle.riddle.answer,
                        Acceptable = (bundle.riddle.acceptable_answers != null && bundle.riddle.acceptable_answers.Length > 0)
                            ? bundle.riddle.acceptable_answers
                            : new[] { bundle.riddle.answer },
                        Hint = bundle.riddle.hint,
                    };

                    if (!string.IsNullOrEmpty(bundle.image_url))
                    {
                        service.StartCoroutine(service.DownloadTexture(
                            bundle.image_url,
                            tex =>
                            {
                                OfflineCache.Save(key, tex, riddle);
                                Debug.Log($"[Seeder] Saved to cache: {key}");
                                done = true;
                            },
                            err =>
                            {
                                Debug.LogWarning("[Seeder] Image Download failed: " + err);
                                done = true;
                            }));
                    }
                    else
                    {
                        Debug.LogWarning("[Seeder] No image_url for this bundle.");
                        done = true;
                    }
                },
                (err) =>
                {
                    Debug.LogWarning("[Seeder] Bundle request failed: " + err);
                    done = true;
                },
                size,
                forceRegen
            );

            while (!done) yield return null;
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("[Seeder] Finished seeding cache.");
    }
}