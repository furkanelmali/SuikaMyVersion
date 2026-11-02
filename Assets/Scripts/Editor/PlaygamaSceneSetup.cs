using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Build-size cleanup for the web release. The game UI itself is built by GameUIBuilder.
/// </summary>
public static class PlaygamaSceneSetup
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";

    // Measured on the first WebGL build: these were ~125 MB of the 168 MB uncompressed assets and never appear in play.
    // The inactive kitchen render and reference screenshot objects sit in the scene; the GUI pack demo Resources folder
    // forces its demo popups/backgrounds into every build although the game never calls Resources.Load on it.
    [MenuItem("Merge&Munch/Playgama/Strip Unused Heavy Content")]
    public static void StripUnusedHeavyContent()
    {
        const string demoResources = "Assets/GUIPackCartoon/Demo/Resources";
        if (AssetDatabase.IsValidFolder(demoResources))
        {
            string error = AssetDatabase.MoveAsset(demoResources, "Assets/GUIPackCartoon/Demo/DemoResources");
            if (!string.IsNullOrEmpty(error))
                throw new System.InvalidOperationException(error);
        }

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            // "Plane" is the inactive backdrop that carries the 9.6 MB screenshot material.
            if (!root.activeSelf && (root.name == "kitchenrender" || root.name == "Plane" || root.name.StartsWith("Screenshot 2024-09-19")))
                Object.DestroyImmediate(root);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("MM_PLAYGAMA_STRIP_OK");
    }
}
