using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ExhibitDataset", menuName = "Scriptable Objects/ExhibitDataset")]
public class ExhibitDataset : ScriptableObject
{
    [Header("Core Lists")]
    [TextArea] public List<string> Subjects = new();
    [TextArea] public List<string> Styles = new();
    [TextArea] public List<string> Lores = new();

    [Header("Optional Weighted Packs")]
    [TextArea] public List<string> SubjectPacks = new();
    [TextArea] public List<string> StylePacks = new();
    [TextArea] public List<string> LorePacks = new();

    public string PickSubject() => Pick(Subjects, SubjectPacks);
    public string PickStyle() => Pick(Styles, StylePacks);
    public string PickLore() => Pick(Lores, LorePacks);
    string Pick(List<string> plain, List<string> weighted)
    {
        var pool = new List<(string, int)>();
        foreach (var p in plain) if (!string.IsNullOrWhiteSpace(p)) pool.Add((p, 1));
        foreach (var w in weighted)
        {
            if (string.IsNullOrWhiteSpace(w)) continue;
            var parts = w.Split('|');
            var item = parts[0].Trim();
            int weight = (parts.Length > 1 && int.TryParse(parts[1], out var v)) ? Mathf.Max(1, v) : 1;
            pool.Add((item, weight));
        }
        if (pool.Count == 0) return "";
        int sum = 0; foreach (var x in pool) sum += x.Item2;
        int r = Random.Range(0, sum), acc = 0;
        foreach (var x in pool) { acc += x.Item2; if (r < acc) return x.Item1; }
        return pool[0].Item1;
    }
}
