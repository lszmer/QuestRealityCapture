#nullable enable

using RealityLog.UI.Coverage;
using UnityEngine;

/// <summary>
/// Automatically sets up Y button binding for fog reset at runtime.
/// This component can be added to any GameObject and will create the binding automatically.
/// </summary>
public sealed class FogSphereButtonSetup : MonoBehaviour
{
    [SerializeField] private bool setupOnStart = true;
    
    private void Start()
    {
        if (setupOnStart)
        {
            SetupYButtonBinding();
        }
    }
    
    public void SetupYButtonBinding()
    {
        // Find FogSphereController
        FogSphereController fogController = FindFirstObjectByType<FogSphereController>();
        if (fogController == null)
        {
            Debug.LogWarning("FogSphereButtonSetup: No FogSphereController found in scene.");
            return;
        }
        
        // Look for existing Y button binding
        GameObject yButtonBinding = GameObject.Find("FogSphereResetBinding_Y");
        
        if (yButtonBinding == null)
        {
            yButtonBinding = new GameObject("FogSphereResetBinding_Y");
            DontDestroyOnLoad(yButtonBinding);
            
            // Add ButtonPressedUnityEvent component using reflection
            var buttonEventType = GetTypeByName("ButtonPressedUnityEvent");
            if (buttonEventType == null)
            {
                Debug.LogError("FogSphereButtonSetup: ButtonPressedUnityEvent type not found!");
                return;
            }
            var buttonEvent = yButtonBinding.AddComponent(buttonEventType) as MonoBehaviour;
            if (buttonEvent != null)
            {
                // Use reflection to get enum values since we can't reference OVRInput directly
                var ovrInputType = GetTypeByName("OVRInput");
                if (ovrInputType != null)
                {
                    var buttonType = ovrInputType.GetNestedType("Button");
                    var controllerType = ovrInputType.GetNestedType("Controller");
                    
                    if (buttonType != null && controllerType != null)
                    {
                        var buttonTwo = System.Enum.Parse(buttonType, "Two") as System.Enum;
                        var controllerLTouch = System.Enum.Parse(controllerType, "LTouch") as System.Enum;
                        
                        if (buttonTwo != null && controllerLTouch != null)
                        {
                            SetButtonProperty(buttonEvent, "button", buttonTwo); // Y button
                            SetButtonProperty(buttonEvent, "controller", controllerLTouch); // Left controller
                        }
                    }
                }
            }
            
            // Add FogSphereResetBinding component using reflection
            var resetBindingType = GetTypeByName("FogSphereResetBinding");
            if (resetBindingType == null)
            {
                Debug.LogError("FogSphereButtonSetup: FogSphereResetBinding type not found!");
                return;
            }
            var resetBinding = yButtonBinding.AddComponent(resetBindingType) as MonoBehaviour;
            if (resetBinding != null)
            {
                SetFogControllerReference(resetBinding, fogController);
            }
            
            Debug.Log("FogSphereButtonSetup: Y button binding created for fog reset.");
        }
        else
        {
            // Update existing binding
            var resetBindingType = GetTypeByName("FogSphereResetBinding");
            var resetBinding = resetBindingType != null ? yButtonBinding.GetComponent(resetBindingType) as MonoBehaviour : null;
            if (resetBinding != null)
            {
                SetFogControllerReference(resetBinding, fogController);
            }
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
    
    private void SetButtonProperty(MonoBehaviour buttonEvent, string propertyName, System.Enum value)
    {
        var buttonEventType = buttonEvent.GetType();
        var field = buttonEventType.GetField(propertyName, 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(buttonEvent, value);
        }
    }
    
    private void SetFogControllerReference(MonoBehaviour resetBinding, FogSphereController fogController)
    {
        if (resetBinding == null) return;
        var resetBindingType = resetBinding.GetType();
        var field = resetBindingType.GetField("fogSphereController", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(resetBinding, fogController);
        }
    }
}

