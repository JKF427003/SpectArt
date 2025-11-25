using UnityEngine;

[ExecuteAlways]
public class ArtFrameAutoBinder : MonoBehaviour
{
    [Header("References")]
    public AIServiceClient service;
    public RiddleUIManager riddleUI;
    public ExhibitDataset dataset;

    [Header("Canvas Fitting")]
    public float forwardOffset = 0.003f;
    public float padding = 0.02f;

    void Start()
    {
        BuildAll();
    }

    [ContextMenu("Rebuild All Frames")]
    public void BuildAll()
    {
        if (!service) service = FindFirstObjectByType<AIServiceClient>();
        if (!riddleUI) riddleUI = FindFirstObjectByType<RiddleUIManager>();

        var frames = GameObject.FindGameObjectsWithTag("Frame");
        foreach (var frame in frames)
            BuildOne(frame.transform);
    }

    void BuildOne(Transform frame)
    {
        for (int i = frame.childCount - 1; i >= 0; i--)
        {
            var child = frame.GetChild(i);
            if (child.name.StartsWith("ArtCanvas") || child.name.StartsWith("Backface"))
                DestroyImmediate(child.gameObject);
        }

        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "ArtCanvas";
        quad.transform.SetParent(frame, false);

        quad.transform.localPosition = Vector3.forward * forwardOffset;
        quad.transform.localRotation = Quaternion.identity;

        var mf = frame.GetComponent<MeshFilter>();
        if (mf && mf.sharedMesh)
        {
            var size = mf.sharedMesh.bounds.size;
            quad.transform.localScale = new Vector3(size.x - padding, size.y - padding, 1f);
        }

        var ctrl = quad.AddComponent<AIExhibitController>();
        ctrl.service = service;
        ctrl.riddleUI = riddleUI;
        ctrl.offlineOnly = false;

        if (dataset)
        {
            ctrl.subject = dataset.PickSubject();
            ctrl.style = dataset.PickStyle();
            ctrl.lore = dataset.PickLore();
        }

        quad.AddComponent<MakeDoubleSided>();

        var col = quad.GetComponent<Collider>();
        if (col) DestroyImmediate(col);
    }
}