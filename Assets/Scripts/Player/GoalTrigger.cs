using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    public GameObject winUi;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.GetComponent<CharacterController>()) return;
        Debug.Log("[SpectArt] Reached EndPoint!");
        if (winUi) winUi.SetActive(true);
    }
}
