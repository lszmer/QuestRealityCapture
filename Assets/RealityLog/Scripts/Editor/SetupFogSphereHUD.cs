#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using RealityLog.UI.Coverage;

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
        
        EditorUtility.SetDirty(fogSphereHUD);
        Debug.Log("FogSphereHUD setup complete!");
    }
}
#endif

