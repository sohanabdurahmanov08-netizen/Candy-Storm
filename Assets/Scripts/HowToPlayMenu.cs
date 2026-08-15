using UnityEngine;
using UnityEngine.InputSystem;

public class HowToPlayMenu : MonoBehaviour
{
    void Update()
    {
        bool clicked = false;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            clicked = true;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            clicked = true;

        if (clicked)
        {
            gameObject.SetActive(false);
        }
    }
}
