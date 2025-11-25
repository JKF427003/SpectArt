using UnityEngine;

public class InputLock : MonoBehaviour
{
    public SimpleFPSController_IS fps;

    public void Lock()
    {
        if (fps) fps.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1.0f;
    }

    public void Unlock()
    {
        if (fps) fps.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
