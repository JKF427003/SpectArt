using UnityEngine;

[ExecuteAlways, DisallowMultipleComponent]
public class MakeDoubleSided : MonoBehaviour
{
    [SerializeField, HideInInspector] GameObject backface;

    void OnEnable() { EnsureBackface(); }
    void OnDestroy() { if (backface) DestroyImmediate(backface); }

    void EnsureBackface()
    {
        var mf = GetComponent<MeshFilter>();
        var mr = GetComponent<MeshRenderer>();
        if (!mf || !mr || !mf.sharedMesh) return;

        if (!backface)
        {
            var t = transform.Find("Backface");
            backface = t ? t.gameObject : new GameObject("Backface");
            if (!t) backface.transform.SetParent(transform, false);
            if (!backface.TryGetComponent<MeshFilter>(out _)) backface.AddComponent<MeshFilter>();
            if (!backface.TryGetComponent<MeshRenderer>(out _)) backface.AddComponent<MeshRenderer>();
        }

        backface.GetComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
        var bmr = backface.GetComponent<MeshRenderer>();
        bmr.sharedMaterial = mr.sharedMaterial;
        bmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        bmr.receiveShadows = false;

        backface.transform.localPosition = Vector3.zero;
        backface.transform.localRotation = Quaternion.Euler(0, 180, 0);
        backface.transform.localScale = Vector3.one;
    }
}