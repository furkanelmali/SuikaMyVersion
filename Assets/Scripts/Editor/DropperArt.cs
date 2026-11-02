using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Draws the bunny tray dropper sprite from the design canvas ("Bunny tray dropper"). Shapes use the design's
/// 300x240 SVG coordinates (y down) and are painted back to front at 2x.
/// </summary>
public static class DropperArt
{
    public const string SpriteName = "bd_bunny_tray";
    public const float Width = 300f, Height = 240f;
    // Share of the sprite's width taken by the tray rim (x 32..268), used to size it against the old model.
    public const float TrayWidthShare = 236f / 300f;
    // The hanger's bottom edge, measured up from the sprite's bottom.
    public const float HangerFromBottom = 12f / 240f;
    const int Scale = 2;

    static readonly Color Cream = Hex("FFF7EC"), CreamLip = Hex("E6C9A6"), Pink = Hex("F6A9B8"), Blush = Hex("F4A9C0");
    static readonly Color Coral = Hex("EE6A4C"), CoralLip = Hex("C24E35"), Cocoa = Hex("5A3A2E"), Nose = Hex("E9839A");
    static readonly Color Butter = Hex("FFC94D"), ButterLip = Hex("D9A22E");

    delegate float Shape(Vector2 p);

    [MenuItem("Merge&Munch/UI/Generate Dropper Sprite")]
    public static void Generate()
    {
        var layers = new List<(Shape shape, Color color)>();
        foreach (var (cx, angle) in new[] { (108f, -12f), (192f, 12f) })
        {
            layers.Add((Rot(RoundRect(cx - 22, 7, 44, 110, 22), cx, 62, angle), CreamLip));
            layers.Add((Rot(RoundRect(cx - 22, 2, 44, 110, 22), cx, 62, angle), Cream));
            layers.Add((Rot(RoundRect(cx - 10, 22, 20, 74, 10), cx, 62, angle), Pink));
        }
        layers.Add((RoundRect(48, 112, 204, 96, 48), CreamLip));
        layers.Add((RoundRect(48, 104, 204, 96, 48), Cream));
        layers.Add((RoundRect(32, 94, 236, 30, 15), CoralLip));
        layers.Add((RoundRect(32, 88, 236, 30, 15), Coral));
        foreach (float x in new[] { 60f, 240f })
        {
            layers.Add((Ellipse(x, 114, 18, 14), CreamLip));
            layers.Add((Ellipse(x, 110, 18, 14), Cream));
        }
        layers.Add((Ellipse(96, 166, 14, 8), Blush));
        layers.Add((Ellipse(204, 166, 14, 8), Blush));
        layers.Add((Circle(118, 150, 9), Cocoa));
        layers.Add((Circle(182, 150, 9), Cocoa));
        layers.Add((Circle(115, 147, 3), Color.white));
        layers.Add((Circle(179, 147, 3), Color.white));
        layers.Add((Ellipse(150, 158, 5, 4), Nose));
        layers.Add((Smile(143, 164, 7, 1.75f), Cocoa));
        layers.Add((Smile(157, 164, 7, 1.75f), Cocoa));
        layers.Add((Circle(150, 216, 12), ButterLip));
        layers.Add((Circle(150, 212, 12), Butter));

        int w = (int)Width * Scale, h = (int)Height * Scale;
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Pixel centre in SVG space (y down).
                var p = new Vector2((x + 0.5f) / Scale, (h - y - 0.5f) / Scale);
                Color acc = new Color(0, 0, 0, 0); // premultiplied
                foreach (var (shape, color) in layers)
                {
                    float a = UISpriteFactory.Coverage(shape(p) * Scale) * color.a;
                    if (a <= 0f)
                        continue;
                    acc = new Color(color.r * a + acc.r * (1 - a), color.g * a + acc.g * (1 - a), color.b * a + acc.b * (1 - a), a + acc.a * (1 - a));
                }
                Color straight = acc.a > 0 ? new Color(acc.r / acc.a, acc.g / acc.a, acc.b / acc.a, acc.a) : acc;
                pixels[y * w + x] = straight;
            }
        }

        UISpriteFactory.Save(SpriteName, w, h, pixels);
        AssetDatabase.Refresh();
        var importer = (TextureImporter)AssetImporter.GetAtPath($"{UISpriteFactory.Folder}/{SpriteName}.png");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = true; // drawn small on phones
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Trilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        Debug.Log("MM_DROPPER_SPRITE_OK");
    }

    public static Sprite Load() => AssetDatabase.LoadAssetAtPath<Sprite>($"{UISpriteFactory.Folder}/{SpriteName}.png");

    // ---------- shapes (SVG units) ----------

    static Shape RoundRect(float x, float y, float w, float h, float r) =>
        p => UISpriteFactory.RoundedRectDistance(p.x, p.y, x, y, w, h, r);

    // Rotate a shape by 'degrees' about (cx, cy), like SVG's rotate(a cx cy).
    static Shape Rot(Shape shape, float cx, float cy, float degrees)
    {
        float rad = -degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
        return p =>
        {
            float dx = p.x - cx, dy = p.y - cy;
            return shape(new Vector2(cx + dx * cos - dy * sin, cy + dx * sin + dy * cos));
        };
    }

    static Shape Ellipse(float cx, float cy, float rx, float ry) =>
        p => (new Vector2((p.x - cx) / rx, (p.y - cy) / ry).magnitude - 1f) * Mathf.Min(rx, ry);

    static Shape Circle(float cx, float cy, float r) => p => (p - new Vector2(cx, cy)).magnitude - r;

    // Lower half of a circle stroked with round caps: one bump of the "w" mouth.
    static Shape Smile(float cx, float cy, float r, float halfStroke) => p =>
    {
        float d = p.y >= cy
            ? Mathf.Abs((p - new Vector2(cx, cy)).magnitude - r)
            : Mathf.Min((p - new Vector2(cx - r, cy)).magnitude, (p - new Vector2(cx + r, cy)).magnitude);
        return d - halfStroke;
    };

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}
