using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Scene dressing built from code: the full-bleed kitchen backdrop (replaces the fixed-width background image)
/// and the visible container — posts on the invisible walls, the dashed limit line at the loss height and the
/// drop guide. Positions come from the scene's own colliders. Called by GameUIBuilder.Build.
/// </summary>
public static class KitchenSceneBuilder
{
    static readonly Color Cream = Hex("FFF7EC"), Butter = Hex("FFC94D"), WoodDark = Hex("C9803A"), PostColor = Hex("F2A07F");

    public static void BuildInto(Scene scene, ObjectSpawner spawner)
    {
        BackdropFactory.GenerateAll();
        BuildBackdrop(scene);
        BuildContainer(scene, spawner);
        BuildDropper(spawner);
    }

    // ---------- dropper ----------

    // Swaps the grey 3D cat tray for the flat bunny tray sprite, sized and placed from the old model's bounds.
    // The model's renderer is only switched off; its collider and the PlayerController stay as they were.
    static void BuildDropper(ObjectSpawner spawner)
    {
        DropperArt.Generate();
        PlayerController player = Object.FindObjectOfType<PlayerController>(true);
        // The visible cat tray is a child model; the dropper's own capsule renderer was already off.
        MeshRenderer model = null;
        foreach (MeshRenderer candidate in player.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (candidate.gameObject.name.StartsWith("Cute_Stylized_tray"))
                model = candidate;
        }
        if (model == null)
            throw new System.InvalidOperationException("The dropper's tray model was not found under " + player.name);
        Transform old = player.transform.Find("BunnyTray");
        if (old != null)
            Object.DestroyImmediate(old.gameObject);

        model.enabled = true;
        Bounds bounds = model.bounds;
        model.enabled = false;

        var tray = new GameObject("BunnyTray");
        tray.transform.SetParent(player.transform, false);
        var renderer = tray.AddComponent<SpriteRenderer>();
        renderer.sprite = DropperArt.Load();

        // The tray rim matches the old bowl's width; ears and hanger extend beyond it.
        float width = bounds.size.x * 1.15f / DropperArt.TrayWidthShare;
        float height = width * DropperArt.Height / DropperArt.Width;
        float nativeWidth = renderer.sprite.bounds.size.x;
        Vector3 lossy = player.transform.lossyScale;
        tray.transform.localScale = new Vector3(width / nativeWidth / lossy.x, width / nativeWidth / lossy.y, 1f);
        // Hanger bottom a little under the bowl's bottom; drawn just behind the held piece's plane.
        float centreY = bounds.min.y - height * 0.02f + height * (0.5f - DropperArt.HangerFromBottom);
        tray.transform.position = new Vector3(bounds.center.x, centreY, spawner.spawnPosition.position.z + 0.6f);
        EditorUtility.SetDirty(player.gameObject);
        Debug.Log($"MM_DROPPER bowl {bounds.size} tray {width:F2}x{height:F2}");
    }

    // ---------- backdrop ----------

    static void BuildBackdrop(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Canvas canvas = root.GetComponent<Canvas>();
            // The old fixed-width background lived on the camera-space "Canvas".
            if (root.name == "Backdrop" || (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera))
                Object.DestroyImmediate(root);
        }

        Camera camera = Camera.main;
        var go = new GameObject("Backdrop", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        go.layer = LayerMask.NameToLayer("UI");
        var canvasComp = go.GetComponent<Canvas>();
        canvasComp.renderMode = RenderMode.ScreenSpaceCamera;
        canvasComp.worldCamera = camera;
        canvasComp.planeDistance = camera != null ? camera.farClipPlane - 2f : 150f;
        canvasComp.sortingOrder = -10;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(390, 844);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f; // bands keep their height; wider screens show more repeats
        Transform root2 = go.transform;

        // Wall gradient and wallpaper dots fill everything.
        Band(root2, "Wall", "bd_wall", 0f, 1f, Vector2.zero, Color.white);
        Band(root2, "Wallpaper", "bd_wallpaper", 0f, 1f, new Vector2(64, 64), Cream);

        // Lamp glow behind everything above the counter.
        RectTransform glow = Anchored(root2, "LampGlow", new Vector2(0.5f, 1f), new Vector2(0, -280), new Vector2(560, 600));
        Img(glow, "bd_glow", new Color(1f, 0.93f, 0.67f, 0.75f));

        // Picture rail with scalloped bunting, 70 units from the top.
        RectTransform rail = TopStrip(root2, "Rail", 70, 6);
        Img(rail, "white", Hex("E9A99C"), sprite: false);
        RectTransform trim = TopStrip(root2, "Trim", 76, 22);
        Raw(trim, "bd_trim", new Vector2(32, 22), Color.white);

        // Sparkles spread across the width so wide screens get some too.
        float[,] sparkles = { { 0.06f, 150, 32 }, { 0.2f, 230, 24 }, { 0.33f, 165, 34 }, { 0.67f, 175, 30 }, { 0.8f, 240, 36 }, { 0.94f, 160, 26 }, { 0.44f, 300, 22 }, { 0.58f, 290, 24 } };
        for (int i = 0; i < sparkles.GetLength(0); i++)
        {
            RectTransform s = Anchored(root2, "Sparkle" + i, new Vector2(sparkles[i, 0], 1f), new Vector2(0, -sparkles[i, 1]), Vector2.one * sparkles[i, 2]);
            Img(s, "bd_sparkle", Color.white);
            s.gameObject.AddComponent<Twinkle>();
        }

        // Backsplash tiles, counter top and cabinet doors, bottom to top as fractions of the height.
        Band(root2, "Tiles", "bd_tiles", 0.38f, 0.56f, new Vector2(40, 40), Color.white);
        Band(root2, "Counter", "bd_counter", 0.345f, 0.38f, Vector2.zero, Color.white);
        Band(root2, "Cabinets", "bd_cabinet", 0f, 0.345f, new Vector2(120, 0), Color.white);

        // Pendant lamp hanging in the middle.
        RectTransform cord = Anchored(root2, "Cord", new Vector2(0.5f, 1f), new Vector2(0, -35), new Vector2(6, 70));
        Img(cord, "white", WoodDark, sprite: false);
        RectTransform lamp = Anchored(root2, "Lamp", new Vector2(0.5f, 1f), new Vector2(0, -112), new Vector2(150, 92));
        Img(lamp, "bd_lamp", Color.white);
    }

    static RectTransform Anchored(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    static RectTransform TopStrip(Transform parent, string name, float top, float height)
    {
        RectTransform rect = Anchored(parent, name, new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -top);
        rect.sizeDelta = new Vector2(0, height);
        return rect;
    }

    static void Band(Transform parent, string name, string texture, float bottom, float top, Vector2 tile, Color tint)
    {
        RectTransform rect = Anchored(parent, name, Vector2.zero, Vector2.zero, Vector2.zero);
        rect.anchorMin = new Vector2(0f, bottom);
        rect.anchorMax = new Vector2(1f, top);
        rect.sizeDelta = Vector2.zero;
        Raw(rect, texture, tile, tint);
    }

    static void Raw(RectTransform rect, string texture, Vector2 tile, Color tint)
    {
        var raw = rect.gameObject.AddComponent<RawImage>();
        raw.texture = BackdropFactory.LoadTexture(texture);
        raw.color = tint;
        raw.raycastTarget = false;
        if (tile != Vector2.zero)
            rect.gameObject.AddComponent<TileFill>().tileSize = tile;
    }

    // sprite: a backdrop sprite; otherwise one of the UI shapes ("white").
    static void Img(RectTransform rect, string name, Color tint, bool sprite = true)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite ? BackdropFactory.LoadSprite(name) : UISpriteFactory.Load(name);
        image.color = tint;
        image.raycastTarget = false;
    }

    // ---------- container ----------

    static void BuildContainer(Scene scene, ObjectSpawner spawner)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "Container")
                Object.DestroyImmediate(root);
        }

        GameObject leftWall = GameObject.Find("Environment Test/LittleWalls");
        GameObject rightWall = GameObject.Find("Environment Test/LittleWalls (1)");
        TriggerLoss loss = Object.FindObjectOfType<TriggerLoss>(true);
        if (leftWall == null || rightWall == null || loss == null)
            throw new System.InvalidOperationException("Container walls or the loss trigger are missing.");

        Bounds left = leftWall.GetComponent<Collider>().bounds, right = rightWall.GetComponent<Collider>().bounds;
        if (left.center.x > right.center.x)
            (left, right) = (right, left);
        float innerLeft = left.max.x, innerRight = right.min.x;
        float z = spawner.spawnPosition.position.z;
        float lossY = loss.GetComponent<Collider>().bounds.center.y;

        // Floor: the highest solid surface under the middle of the container.
        Physics.SyncTransforms();
        float floorY = float.NegativeInfinity;
        float midX = (innerLeft + innerRight) * 0.5f;
        foreach (RaycastHit hit in Physics.RaycastAll(new Vector3(midX, lossY + 10f, z), Vector3.down, 80f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.gameObject == leftWall || hit.collider.gameObject == rightWall || hit.point.y >= lossY - 1f)
                continue;
            floorY = Mathf.Max(floorY, hit.point.y);
        }
        if (float.IsNegativeInfinity(floorY))
            floorY = left.min.y;

        var container = new GameObject("Container");
        Material post = SaveMaterial("Post", "Standard", PostColor, 0.4f);
        Material cap = SaveMaterial("PostCap", "Standard", Butter, 0.5f);
        const float radius = 0.3f;
        foreach (float x in new[] { innerLeft - radius, innerRight + radius })
        {
            float height = lossY + 0.4f - floorY;
            GameObject pillar = Primitive(PrimitiveType.Capsule, "Post", container.transform, post);
            pillar.transform.position = new Vector3(x, floorY + height * 0.5f, z);
            pillar.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            GameObject knob = Primitive(PrimitiveType.Sphere, "Cap", container.transform, cap);
            knob.transform.position = new Vector3(x, floorY + height, z);
            knob.transform.localScale = Vector3.one * radius * 3f;
        }

        // Dashed limit line at the loss height, slightly in front of the pieces' plane.
        Material dash = SaveMaterial("Dash", "Sprites/Default", Color.white, 0f, BackdropFactory.LoadTexture("bd_dash"));
        var limitGo = new GameObject("LimitLine");
        limitGo.transform.SetParent(container.transform, false);
        LineRenderer limit = Line(limitGo, dash, 0.14f);
        limit.SetPosition(0, new Vector3(innerLeft, lossY, z - 0.6f));
        limit.SetPosition(1, new Vector3(innerRight, lossY, z - 0.6f));
        limitGo.AddComponent<LimitLine>().line = limit;

        // Drop guide under the held piece.
        PlayerController player = Object.FindObjectOfType<PlayerController>(true);
        Transform oldGuide = player.transform.Find("AimLine");
        if (oldGuide != null)
            Object.DestroyImmediate(oldGuide.gameObject);
        var aimGo = new GameObject("AimLine");
        aimGo.transform.SetParent(player.transform, false);
        LineRenderer aim = Line(aimGo, dash, 0.16f);
        aim.startColor = aim.endColor = new Color(1f, 1f, 1f, 0.9f);
        AimGuide guide = player.GetComponent<AimGuide>();
        if (guide == null)
            guide = player.gameObject.AddComponent<AimGuide>();
        guide.line = aim;
        EditorUtility.SetDirty(player.gameObject);

        // Portrait phones: widen the view so both posts stay on screen.
        Camera camera = Camera.main;
        CameraFit fit = camera.GetComponent<CameraFit>();
        if (fit == null)
        {
            fit = camera.gameObject.AddComponent<CameraFit>();
            fit.baseFieldOfView = camera.fieldOfView;
        }
        fit.containerLeft = innerLeft - radius * 2f;
        fit.containerRight = innerRight + radius * 2f;
        fit.planeZ = z;
        EditorUtility.SetDirty(camera);

        Debug.Log($"MM_CONTAINER walls {innerLeft:F2}..{innerRight:F2} floor {floorY:F2} loss {lossY:F2} z {z:F2}");
    }

    static LineRenderer Line(GameObject go, Material material, float width)
    {
        var line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.sharedMaterial = material;
        line.textureMode = LineTextureMode.Tile;
        line.startWidth = line.endWidth = width;
        line.numCapVertices = 0;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.alignment = LineAlignment.View;
        return line;
    }

    static GameObject Primitive(PrimitiveType type, string name, Transform parent, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, true);
        Object.DestroyImmediate(go.GetComponent<Collider>()); // visual only: the original walls keep the physics
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    static Material SaveMaterial(string name, string shader, Color color, float gloss, Texture texture = null)
    {
        string path = $"{UISpriteFactory.Folder}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = Shader.Find(shader);
        material.color = color;
        if (material.HasProperty("_Glossiness"))
            material.SetFloat("_Glossiness", gloss);
        if (texture != null)
            material.mainTexture = texture;
        EditorUtility.SetDirty(material);
        return material;
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}
