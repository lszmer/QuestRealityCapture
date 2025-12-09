#nullable enable

using UnityEngine;

namespace RealityLog.UI.Coverage
{
    /// <summary>
    /// Automatically sets up recording start connections at runtime.
    /// This ensures fog resets when recording starts via X button.
    /// </summary>
    public sealed class FogSphereHUDSetup : MonoBehaviour
    {
        [SerializeField] private bool setupOnStart = true;
        
        private void Start()
        {
            if (setupOnStart)
            {
                SetupRecordingStartConnections();
            }
        }
        
        public void SetupRecordingStartConnections()
        {
            // Find CoverageRecordingStartHandler
            var recordingHandler = FindFirstObjectByType<CoverageRecordingStartHandler>();
            if (recordingHandler == null)
            {
                // Try to find on FogSphereHUD
                GameObject fogSphereHUD = GameObject.Find("FogSphereHUD");
                if (fogSphereHUD != null)
                {
                    recordingHandler = fogSphereHUD.GetComponent<CoverageRecordingStartHandler>();
                    if (recordingHandler == null)
                    {
                        recordingHandler = fogSphereHUD.AddComponent<CoverageRecordingStartHandler>();
                    }
                }
            }
            
            if (recordingHandler == null)
            {
                Debug.LogWarning("FogSphereHUDSetup: No CoverageRecordingStartHandler found. Fog reset on recording start may not work.");
                return;
            }
            
            // Find all ToggleValueUnityEvent components and ensure they have connectors
            var toggleType = GetTypeByName("ToggleValueUnityEvent");
            if (toggleType == null)
            {
                Debug.LogWarning("FogSphereHUDSetup: ToggleValueUnityEvent type not found. Recording start connection may not work.");
                return;
            }
            
            var allToggles = FindObjectsByType(toggleType, FindObjectsSortMode.None);
            
            foreach (var toggle in allToggles)
            {
                if (!(toggle is MonoBehaviour mb)) continue;
                var toggleGO = mb.gameObject;
                // Use GetComponent with string type name to avoid namespace issues
                var connector = toggleGO.GetComponent(GetTypeByName("RecordingStartFogResetConnector")) as MonoBehaviour;
                
                if (connector == null)
                {
                    var connectorType = GetTypeByName("RecordingStartFogResetConnector");
                    if (connectorType != null)
                    {
                        connector = toggleGO.AddComponent(connectorType) as MonoBehaviour;
                    }
                }
                
                // Set the fog reset handler reference
                if (connector != null)
                {
                    SetFogResetHandlerReference(connector, recordingHandler);
                }
            }
            
            if (allToggles != null && allToggles.Length > 0)
            {
                Debug.Log($"FogSphereHUDSetup: Connected {allToggles.Length} recording toggle(s) to fog reset handler.");
            }
        }
        
        private System.Type? GetTypeByName(string typeName)
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName);
                if (type != null) return type;
            }
            return null;
        }
        
        private void SetFogResetHandlerReference(MonoBehaviour connector, CoverageRecordingStartHandler handler)
        {
            var connectorType = connector.GetType();
            var field = connectorType.GetField("fogResetHandler", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(connector, handler);
            }
        }
    }
}

