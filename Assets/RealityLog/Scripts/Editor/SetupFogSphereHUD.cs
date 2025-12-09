#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using RealityLog.UI.Coverage;

/// <summary>
/// Editor tool for setting up FogSphereHUD. 
/// NOTE: This is OPTIONAL - all setup now happens automatically at runtime.
/// FogSphereController, FogSphereButtonSetup, and RecordingStartFogResetConnector
/// handle all configuration automatically when the scene starts.
/// </summary>
public class SetupFogSphereHUD : EditorWindow
{
    [MenuItem("Tools/Setup Fog Sphere HUD")]
    public static void SetupFogSphere()
    {
        // Find or create FogSphereHUD
        GameObject fogSphereHUD = GameObject.Find("FogSphereHUD");
        
        // Find the CenterEyeAnchor
        Transform centerEyeAnchor = null;
        GameObject cameraRig = GameObject.Find("OVRCameraRig");
        if (cameraRig != null)
        {
            centerEyeAnchor = cameraRig.transform.Find("TrackingSpace/CenterEyeAnchor");
        }
        
        // If CenterEyeAnchor not found, try Camera.main as fallback
        if (centerEyeAnchor == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                centerEyeAnchor = mainCamera.transform;
            }
        }
        
        if (fogSphereHUD == null)
        {
            fogSphereHUD = new GameObject("FogSphereHUD");
            
            if (centerEyeAnchor != null)
            {
                fogSphereHUD.transform.SetParent(centerEyeAnchor);
            }
            
            fogSphereHUD.transform.localPosition = Vector3.zero;
            fogSphereHUD.transform.localRotation = Quaternion.identity;
            fogSphereHUD.transform.localScale = Vector3.one;
        }
        else
        {
            // FogSphereHUD exists - verify and fix parent if needed
            if (centerEyeAnchor != null && fogSphereHUD.transform.parent != centerEyeAnchor)
            {
                fogSphereHUD.transform.SetParent(centerEyeAnchor);
                fogSphereHUD.transform.localPosition = Vector3.zero;
                fogSphereHUD.transform.localRotation = Quaternion.identity;
                fogSphereHUD.transform.localScale = Vector3.one;
                Debug.Log("FogSphereHUD parent updated to CenterEyeAnchor");
            }
        }
        
        // Add CoverageRecordingStartHandler
        CoverageRecordingStartHandler recordingHandler = fogSphereHUD.GetComponent<CoverageRecordingStartHandler>();
        if (recordingHandler == null)
        {
            recordingHandler = fogSphereHUD.AddComponent<CoverageRecordingStartHandler>();
        }
        
        // Add FogSphereController
        FogSphereController fogController = fogSphereHUD.GetComponent<FogSphereController>();
        if (fogController == null)
        {
            fogController = fogSphereHUD.AddComponent<FogSphereController>();
        }
        
        // Set up the recording handler reference
        recordingHandler.GetType().GetField("fogController", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(recordingHandler, fogController);
        
        // Create FogSphere child
        Transform fogSphereTransform = fogSphereHUD.transform.Find("FogSphere");
        GameObject fogSphere;
        
        if (fogSphereTransform == null)
        {
            fogSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fogSphere.name = "FogSphere";
            fogSphere.transform.SetParent(fogSphereHUD.transform);
            fogSphere.transform.localPosition = Vector3.zero;
            fogSphere.transform.localRotation = Quaternion.identity;
            fogSphere.transform.localScale = Vector3.one * 2f; // diameter = 2 * radius (radius = 1)
            
            // Remove the default collider if not needed, or keep it
            // SphereCollider collider = fogSphere.GetComponent<SphereCollider>();
            // if (collider != null) DestroyImmediate(collider);
        }
        else
        {
            fogSphere = fogSphereTransform.gameObject;
        }
        
        // Load materials and shaders
        Material fogMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/RealityLog/Materials/FogSphere_Mat.mat");
        ComputeShader fogMaskCompute = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/RealityLog/Shaders/FogMaskStamp.compute");
        
        // Configure FogSphereController
        SerializedObject so = new SerializedObject(fogController);
        // Use CenterEyeAnchor if available, otherwise fall back to Camera.main
        Transform headTransform = centerEyeAnchor != null ? centerEyeAnchor : Camera.main?.transform;
        so.FindProperty("headTransform").objectReferenceValue = headTransform;
        so.FindProperty("sphereTransform").objectReferenceValue = fogSphere.transform;
        so.FindProperty("sphereRenderer").objectReferenceValue = fogSphere.GetComponent<Renderer>();
        so.FindProperty("fogMaterial").objectReferenceValue = fogMaterial;
        so.FindProperty("fogMaskCompute").objectReferenceValue = fogMaskCompute;
        so.FindProperty("startVisible").boolValue = true;
        so.FindProperty("sphereRadius").floatValue = 1f;
        so.FindProperty("maskResolution").vector2IntValue = new Vector2Int(512, 256);
        so.FindProperty("brushAngleDegrees").floatValue = 30f;
        so.FindProperty("brushFeatherDegrees").floatValue = 5f;
        so.FindProperty("bottomClearAngleDegrees").floatValue = 50f;
        so.FindProperty("topClearAngleDegrees").floatValue = 30f;
        so.FindProperty("viewOffsetDegrees").vector2Value = new Vector2(0, -13);
        so.ApplyModifiedProperties();
        
        // Set the material on the renderer
        Renderer renderer = fogSphere.GetComponent<Renderer>();
        if (renderer != null && fogMaterial != null)
        {
            renderer.sharedMaterial = fogMaterial;
        }
        
        // Set recording handler reference
        SerializedObject recordingSO = new SerializedObject(recordingHandler);
        recordingSO.FindProperty("fogController").objectReferenceValue = fogController;
        recordingSO.ApplyModifiedProperties();
        
        // Setup Y button binding for fog reset (if not already set up)
        SetupYButtonBinding(fogController);
        
        // Setup X button to call OnRecordingStarted when recording begins
        SetupRecordingStartConnection(recordingHandler);
        
        EditorUtility.SetDirty(fogSphereHUD);
        Debug.Log("FogSphereHUD setup complete!");
    }
    
    private static void SetupYButtonBinding(FogSphereController fogController)
    {
        // Look for existing Y button binding GameObject
        GameObject yButtonBinding = GameObject.Find("FogSphereResetBinding_Y");
        
        if (yButtonBinding == null)
        {
            yButtonBinding = new GameObject("FogSphereResetBinding_Y");
            
            // Add ButtonPressedUnityEvent component
            var buttonEvent = yButtonBinding.AddComponent<ButtonPressedUnityEvent>();
            var buttonEventSO = new SerializedObject(buttonEvent);
            // OVRInput.Button.Two = Y button on left controller
            buttonEventSO.FindProperty("button").intValue = 2; // OVRInput.Button.Two
            // OVRInput.Controller.LTouch = Left controller
            buttonEventSO.FindProperty("controller").intValue = 1; // OVRInput.Controller.LTouch
            buttonEventSO.ApplyModifiedProperties();
            
            // Add FogSphereResetBinding component
            var resetBinding = yButtonBinding.AddComponent<FogSphereResetBinding>();
            var resetBindingSO = new SerializedObject(resetBinding);
            resetBindingSO.FindProperty("fogSphereController").objectReferenceValue = fogController;
            resetBindingSO.ApplyModifiedProperties();
            
            EditorUtility.SetDirty(yButtonBinding);
            Debug.Log("Y button binding for fog reset created!");
        }
        else
        {
            // Update existing binding
            var resetBinding = yButtonBinding.GetComponent<FogSphereResetBinding>();
            if (resetBinding != null)
            {
                var resetBindingSO = new SerializedObject(resetBinding);
                resetBindingSO.FindProperty("fogSphereController").objectReferenceValue = fogController;
                resetBindingSO.ApplyModifiedProperties();
            }
        }
    }
    
    private static void SetupRecordingStartConnection(CoverageRecordingStartHandler recordingHandler)
    {
        // Find all ToggleValueUnityEvent components in the scene
        var allToggles = Object.FindObjectsByType<ToggleValueUnityEvent>(FindObjectsSortMode.None);
        
        foreach (var toggle in allToggles)
        {
            // Check if this toggle is the recording toggle by looking for PoseLogger.StartLogging connections
            // or by checking if it's connected to a ButtonPressedUnityEvent with button 260
            var toggleGO = toggle.gameObject;
            
            // Add RecordingStartFogResetConnector if not already present
            var connector = toggleGO.GetComponent<RecordingStartFogResetConnector>();
            if (connector == null)
            {
                connector = toggleGO.AddComponent<RecordingStartFogResetConnector>();
                var connectorSO = new SerializedObject(connector);
                connectorSO.FindProperty("fogResetHandler").objectReferenceValue = recordingHandler;
                connectorSO.ApplyModifiedProperties();
                EditorUtility.SetDirty(toggleGO);
                Debug.Log($"Added RecordingStartFogResetConnector to {toggleGO.name}");
            }
            else
            {
                // Update existing connector
                var connectorSO = new SerializedObject(connector);
                connectorSO.FindProperty("fogResetHandler").objectReferenceValue = recordingHandler;
                connectorSO.ApplyModifiedProperties();
            }
        }
        
        if (allToggles.Length == 0)
        {
            Debug.LogWarning("No ToggleValueUnityEvent found in scene. " +
                           "Please manually add RecordingStartFogResetConnector to the recording toggle GameObject.");
        }
        else
        {
            Debug.Log("Recording start connection setup complete!");
        }
    }
}
#endif

