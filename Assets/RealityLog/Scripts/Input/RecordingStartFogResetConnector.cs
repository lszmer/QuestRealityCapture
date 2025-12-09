#nullable enable

using RealityLog.UI.Coverage;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Connects the recording start toggle to the fog reset handler.
/// Add this component to the same GameObject as ToggleValueUnityEvent that handles recording.
/// This ensures fog is reset when recording starts via X button.
/// </summary>
[RequireComponent(typeof(ToggleValueUnityEvent))]
public sealed class RecordingStartFogResetConnector : MonoBehaviour
{
    [SerializeField] private CoverageRecordingStartHandler? fogResetHandler;
    private ToggleValueUnityEvent? recordingToggle;

    private void Awake()
    {
        // Try to find the fog reset handler if not assigned
        if (fogResetHandler == null)
        {
            // First try to find on FogSphereHUD
            GameObject fogSphereHUD = GameObject.Find("FogSphereHUD");
            if (fogSphereHUD != null)
            {
                fogResetHandler = fogSphereHUD.GetComponent<CoverageRecordingStartHandler>();
            }
            
            // If not found, search the scene
            if (fogResetHandler == null)
            {
                fogResetHandler = FindFirstObjectByType<CoverageRecordingStartHandler>();
            }
        }
        
        // Get the ToggleValueUnityEvent component
        recordingToggle = GetComponent<ToggleValueUnityEvent>();
    }
    
    private void Start()
    {
        // Ensure we're subscribed even if OnEnable was called before Awake
        if (recordingToggle != null && fogResetHandler != null)
        {
            OnEnable();
        }
    }

    private void OnEnable()
    {
        // Subscribe to the activated event
        if (recordingToggle != null)
        {
            // Access the activated event via reflection since it's private
            var toggleType = typeof(ToggleValueUnityEvent);
            var activatedField = toggleType.GetField("activated", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (activatedField != null)
            {
                var activatedEvent = activatedField.GetValue(recordingToggle) as UnityEvent;
                if (activatedEvent != null)
                {
                    activatedEvent.AddListener(OnRecordingActivated);
                }
            }
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from the activated event
        if (recordingToggle != null)
        {
            var toggleType = typeof(ToggleValueUnityEvent);
            var activatedField = toggleType.GetField("activated", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (activatedField != null)
            {
                var activatedEvent = activatedField.GetValue(recordingToggle) as UnityEvent;
                if (activatedEvent != null)
                {
                    activatedEvent.RemoveListener(OnRecordingActivated);
                }
            }
        }
    }

    private void OnRecordingActivated()
    {
        // Call OnRecordingStarted on the fog reset handler
        fogResetHandler?.OnRecordingStarted();
    }
}

