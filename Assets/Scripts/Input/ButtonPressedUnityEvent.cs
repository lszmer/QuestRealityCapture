# nullable enable

using UnityEngine;
using UnityEngine.Events;

public class ButtonPressedUnityEvent : MonoBehaviour
{
    [SerializeField] private OVRInput.Button button = default!;
    [SerializeField] private OVRInput.Controller controller = default!;
    [SerializeField] private UnityEvent onButtonPressed = default!;

    public void AddListener(UnityAction callback)
    {
        onButtonPressed.AddListener(callback);
    }

    public void RemoveListener(UnityAction callback)
    {
        onButtonPressed.RemoveListener(callback);
    }

    void Update()
    {
        if (OVRInput.GetDown(button, controller))
        {
            onButtonPressed?.Invoke();
        }
    }
}
