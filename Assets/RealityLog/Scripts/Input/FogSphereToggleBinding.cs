#nullable enable

using RealityLog.UI.Coverage;
using UnityEngine;

[RequireComponent(typeof(ButtonPressedUnityEvent))]
public sealed class FogSphereToggleBinding : MonoBehaviour
{
    [SerializeField] private FogSphereController? fogSphereController;
    [SerializeField] private ButtonPressedUnityEvent? buttonEvent;

    private void Awake()
    {
        buttonEvent ??= GetComponent<ButtonPressedUnityEvent>();

        if (fogSphereController == null)
        {
            fogSphereController = GetComponentInParent<FogSphereController>();
        }

        if (fogSphereController == null)
        {
            Debug.LogWarning($"{nameof(FogSphereToggleBinding)} missing FogSphereController reference.", this);
        }
    }

    private void OnEnable()
    {
        buttonEvent?.AddListener(ToggleFogSphere);
    }

    private void OnDisable()
    {
        buttonEvent?.RemoveListener(ToggleFogSphere);
    }

    private void ToggleFogSphere()
    {
        if (fogSphereController == null)
        {
            Debug.LogWarning($"{nameof(FogSphereToggleBinding)} missing FogSphereController reference.", this);
            return;
        }

        // X press should never hide during recording; when forced, just reset.
        if (fogSphereController.IsForcedVisible)
        {
            fogSphereController.ResetFog();
            return;
        }

        // Outside recording, treat X as "show + reset" rather than hide.
        if (!fogSphereController.IsSphereVisible)
        {
            fogSphereController.SetSphereVisible(true);
        }
        else
        {
            fogSphereController.ResetFog();
        }
    }
}

