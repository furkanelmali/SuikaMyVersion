using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Renders the game's food models to transparent PNGs for store covers
/// (Documentation/Playgama/Covers/renders). Needs a graphics device: run without -nographics.
/// </summary>
public static class CoverArtRenderer
{
    static readonly string[] Foods = { "Watermelon", "Pumpkin", "Cake", "Burger", "Donut_02", "Bread", "Apple", "Pear", "Carrot", "Cookie", "CoffeCup" };
    const int Size = 768;

    [MenuItem("Merge&Munch/Playgama/Render Cover Foods")]
    public static void Render()
    {
        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Documentation", "Playgama", "Covers", "renders"));
        Directory.CreateDirectory(folder);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var key = new GameObject("Key").AddComponent<Light>();
        key.type = LightType.Directional;
        key.intensity = 1.25f;
        key.color = new Color(1f, 0.96f, 0.9f);
        key.transform.rotation = Quaternion.Euler(35, -35, 0);
        var fill = new GameObject("Fill").AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = 0.55f;
        fill.color = new Color(1f, 0.85f, 0.85f);
        fill.transform.rotation = Quaternion.Euler(10, 150, 0);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.58f, 0.6f);

        var camera = new GameObject("Camera").AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0, 0, 0, 0);
        camera.fieldOfView = 24f;
        camera.allowMSAA = true;
        var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
        camera.targetTexture = target;

        foreach (string food in Foods)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/{food}.prefab");
            if (prefab == null)
                continue;
            GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            item.transform.position = Vector3.zero;
            item.transform.rotation = Quaternion.Euler(-12, 28, 0);
            foreach (ParticleSystem particles in item.GetComponentsInChildren<ParticleSystem>(true))
                particles.gameObject.SetActive(false);

            Bounds bounds = new Bounds(item.transform.position, Vector3.zero);
            bool any = false;
            foreach (Renderer r in item.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy)
                    continue;
                bounds = any ? Encapsulate(bounds, r.bounds) : r.bounds;
                any = true;
            }

            float radius = bounds.extents.magnitude;
            float distance = radius / Mathf.Sin(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 0.66f; // the bounding sphere overstates these shapes
            camera.transform.position = bounds.center + new Vector3(0, radius * 0.15f, -distance);
            camera.transform.LookAt(bounds.center);
            camera.nearClipPlane = Mathf.Max(0.01f, distance - radius * 2f);
            camera.farClipPlane = distance + radius * 2f;

            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            image.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(folder, food.ToLowerInvariant() + ".png"), image.EncodeToPNG());
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(item);
        }

        camera.targetTexture = null;
        target.Release();
        Debug.Log("MM_COVER_RENDERS_OK " + folder);
    }

    static Bounds Encapsulate(Bounds a, Bounds b)
    {
        a.Encapsulate(b);
        return a;
    }
}
