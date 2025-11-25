using UnityEngine;

public class ExhibitDatasetBinder : MonoBehaviour
{
    [Header("References")]
    public ExhibitDataset dataset;
    public AIExhibitController controller;

    [Header("Options")]
    public bool pickOnAwake = true;
    public bool onlyIfEmpty = true;

    private void Awake()
    {
        if (!pickOnAwake || dataset == null || controller == null) return;

        if (onlyIfEmpty)
        {
            if (!string.IsNullOrWhiteSpace(controller.subject) || !string.IsNullOrWhiteSpace(controller.style) || !string.IsNullOrWhiteSpace(controller.lore))
            {
                return;
            }
        }

        controller.subject = dataset.PickSubject();
        controller.style = dataset.PickStyle();
        controller.lore = dataset.PickLore();
    }
}
