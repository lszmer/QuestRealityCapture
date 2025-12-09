#nullable enable

using RealityLog.UI.Coverage;
using UnityEngine;

[RequireComponent(typeof(ButtonPressedUnityEvent))]
public sealed class FogSphereResetBinding : MonoBehaviour
{
    [SerializeField] private FogSphereController? fogSphereController;

    private ButtonPressedUnityEvent? buttonEvent;

    private void Awake()
    {
        buttonEvent = GetComponent<ButtonPressedUnityEvent>();

        if (fogSphereController == null)
        {
            Debug.LogWarning($"{nameof(FogSphereResetBinding)} lacks a FogSphereController reference. Assign one in the inspector.", this);
        }
    }

    private void OnEnable()
    {
        buttonEvent?.AddListener(ResetFog);
    }

    private void OnDisable()
    {
        buttonEvent?.RemoveListener(ResetFog);
    }

    private void ResetFog()
    {
        if (fogSphereController == null)
        {
            Debug.LogWarning($"{nameof(FogSphereResetBinding)} missing FogSphereController reference.", this);
            return;
        }

        // Always reset fog when Y button is pressed, regardless of visibility
        fogSphereController.ResetFog();
    }
}

