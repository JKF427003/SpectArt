using UnityEngine;

public class ExhibitSpawner : MonoBehaviour
{
    public ExhibitDataset dataset;
    public AIServiceClient service;
    public RiddleUIManager riddleUI;
    public GameObject artFramePrefab;
    public int count = 3;
    public Vector3 startPos = new(0, 1.2f, 3f);
    public float spacing = 2.0f;

    void Start()
    {
        for (int i = 0; i < count; i++)
        {
            var pos = startPos + new Vector3(i * spacing, 0, 0);
            var go = Instantiate(artFramePrefab, pos, Quaternion.Euler(0, 180, 0), transform);
            var ctrl = go.GetComponent<AIExhibitController>();

            ctrl.service = service;
            ctrl.riddleUI = riddleUI;

            ctrl.subject = dataset.Subjects[Random.Range(0, dataset.Subjects.Count)];
            ctrl.style = dataset.Styles[Random.Range(0, dataset.Styles.Count)];
            ctrl.lore = dataset.Lores[Random.Range(0, dataset.Lores.Count)];
        }
    }
}
