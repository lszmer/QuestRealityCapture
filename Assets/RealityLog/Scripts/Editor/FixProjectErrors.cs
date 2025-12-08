#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using RealityLog.UI.Coverage;

public class FixProjectErrors
{
    [MenuItem("Tools/Fix Project Errors")]
    public static void FixAllErrors()
    {
        Debug.Log("Starting project error fixes...");
        
        // 1. Fix FogSphereHUD parent
        FixFogSphereHUDParent();
        
        // 2. Reimport shader graphs to fix mapping errors
        ReimportShaderGraphs();
        
        Debug.Log("Project error fixes complete!");
    }
    
    private static void FixFogSphereHUDParent()
    {
        Debug.Log("Fixing FogSphereHUD parent...");
        
        // Use the existing setup script logic
        SetupFogSphereHUD.SetupFogSphere();
    }
    
    private static void ReimportShaderGraphs()
    {
        Debug.Log("Reimporting shader graphs to fix mapping errors...");
        
        // Find all shader graph files
        string[] shaderGraphGuids = AssetDatabase.FindAssets("t:ShaderGraph");
        
        int reimported = 0;
        foreach (string guid in shaderGraphGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            
            // Only reimport TextMesh Pro shader graphs (they're the ones causing errors)
            if (path.Contains("TextMesh Pro"))
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                reimported++;
            }
        }
        
        Debug.Log($"Reimported {reimported} shader graph files.");
        
        // Force refresh
        AssetDatabase.Refresh();
    }
}
#endif

