using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class CachePathHelper : MonoBehaviour
{
    [ContextMenu("Print Persistent Data Path")]
    void PrintPath()
    {
        string path = Application.persistentDataPath;
        Debug.Log($"PersistenDataPath = {path}");

        #if UNITY_EDITOR
                EditorUtility.RevealInFinder(path);
        #endif
    }

    private void Start()
    {
        Debug.Log($"[SpectArt] Cache folder → {Application.persistentDataPath}/SpectArtCache/");
    }
}
