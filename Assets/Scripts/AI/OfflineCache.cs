using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class OfflineCache
{
    // Root folder for all cached bundles
    static string Root => Path.Combine(Application.persistentDataPath, "SpectArtCache");

    // ----------------------------------------------------------------------
    // SAVE
    // ----------------------------------------------------------------------
    public static void Save(string key, Texture2D png, RiddleData rid)
    {
        if (png == null || rid == null)
        {
            Debug.LogWarning("[OfflineCache] Tried to save with null texture or riddle – skipping.");
            return;
        }

        Directory.CreateDirectory(Root);

        var imgPath = Path.Combine(Root, key + ".png");
        var jsonPath = Path.Combine(Root, key + ".json");

        File.WriteAllBytes(imgPath, png.EncodeToPNG());
        File.WriteAllText(jsonPath, JsonUtility.ToJson(rid, prettyPrint: true));

        // Optional debug:
        // Debug.Log($"[OfflineCache] Saved bundle for key={key} at {imgPath}");
    }

    // ----------------------------------------------------------------------
    // LOAD BY KEY
    // ----------------------------------------------------------------------
    public static bool TryLoad(string key, out Texture2D tex, out RiddleData rid)
    {
        tex = null;
        rid = null;

        var imgPath = Path.Combine(Root, key + ".png");
        var jsonPath = Path.Combine(Root, key + ".json");

        if (!File.Exists(imgPath) || !File.Exists(jsonPath))
            return false;

        var bytes = File.ReadAllBytes(imgPath);
        if (bytes == null || bytes.Length == 0)
            return false;

        tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(bytes))
        {
            Object.Destroy(tex);
            tex = null;
            return false;
        }

        var json = File.ReadAllText(jsonPath);
        if (string.IsNullOrWhiteSpace(json))
            return false;

        rid = JsonUtility.FromJson<RiddleData>(json);

        return (tex != null && rid != null);
    }

    // ----------------------------------------------------------------------
    // LOAD ANY CACHED BUNDLE (fallback for offlineOnly)
    // ----------------------------------------------------------------------
    public static bool TryLoadAny(out Texture2D tex, out RiddleData rid)
    {
        tex = null;
        rid = null;

        if (!Directory.Exists(Root))
            return false;

        // Look for any .png file in the cache
        var pngs = Directory.GetFiles(Root, "*.png", SearchOption.TopDirectoryOnly);
        if (pngs == null || pngs.Length == 0)
            return false;

        // Pick a random cached image
        int index = Random.Range(0, pngs.Length);
        string pngPath = pngs[index];
        string key = Path.GetFileNameWithoutExtension(pngPath);

        // Re-use normal TryLoad so we also get the riddle json
        return TryLoad(key, out tex, out rid);
    }

    // ----------------------------------------------------------------------
    // HELPERS
    // ----------------------------------------------------------------------
    public static bool HasKey(string key)
    {
        var imgPath = Path.Combine(Root, key + ".png");
        return File.Exists(imgPath);
    }

    public static bool HasAny()
    {
        if (!Directory.Exists(Root))
            return false;

        var pngs = Directory.GetFiles(Root, "*.png", SearchOption.TopDirectoryOnly);
        return pngs != null && pngs.Length > 0;
    }

    // ----------------------------------------------------------------------
    // KEY GENERATION
    // ----------------------------------------------------------------------
    public static string KeyFrom(string subject, string style, string lore, string size)
    {
        string normSize = NormalizeSize(size);
        string s = $"{subject}|{style}|{lore}|{normSize}";

        using (var sha = SHA256.Create())
        {
            byte[] data = Encoding.UTF8.GetBytes(s);
            byte[] hash = sha.ComputeHash(data);

            var sb = new StringBuilder(16);
            for (int i = 0; i < 8; i++)
                sb.Append(hash[i].ToString("x2"));

            return sb.ToString();
        }
    }

    static string NormalizeSize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "512x512";

        s = s.ToLowerInvariant();
        if (s is "256x256" or "512x512" or "1024x1024")
            return s;

        if (!int.TryParse(s.Split('x')[0], out int n))
            n = 512;

        if (n <= 256) n = 256;
        else if (n <= 512) n = 512;
        else n = 1024;

        return $"{n}x{n}";
    }
}
