using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Draws the game UI's sprites from code: white shapes (tinted by Image.color) and white icons.
/// Icons use the 24-unit coordinates of the design canvas's SVG icons, rasterised with anti-aliased distance fields.
/// </summary>
public static class UISpriteFactory
{
    public const string Folder = "Assets/UI/Generated";
    const int IconSize = 128;
    public const float RoundedRadius = 24f;

    [MenuItem("Merge&Munch/UI/Generate Sprites")]
    public static void GenerateAll()
    {
        Directory.CreateDirectory(Folder);

        // White shapes tinted by Image.color. "rounded" is 9-sliced with a 24 px corner; Image.pixelsPerUnitMultiplier
        // rescales that corner per element (0.75 gives 32 px card corners).
        WriteShape("rounded", 64, 64, RoundedRadius, new Vector4(25, 25, 25, 25));
        WriteShape("circle", 128, 128, 64f, Vector4.zero);
        WriteShape("white", 8, 8, 0f, new Vector4(3, 3, 3, 3));

        foreach (var icon in Icons)
            WriteIcon(icon.Key, icon.Value);

        AssetDatabase.Refresh();
        ConfigureImporters();
        Debug.Log("MM_UI_SPRITES_OK");
    }

    public static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Folder}/{name}.png");

    // ---------- shapes ----------

    static void WriteShape(string name, int width, int height, float radius, Vector4 border)
    {
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float alpha = Coverage(RoundedRectDistance(x + 0.5f, y + 0.5f, 0, 0, width, height, radius));
                pixels[y * width + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(255 * alpha));
            }
        }
        Save(name, width, height, pixels);
        borders[name] = border;
    }

    internal static float RoundedRectDistance(float px, float py, float x, float y, float w, float h, float r)
    {
        r = Mathf.Min(r, Mathf.Min(w, h) * 0.5f);
        float cx = x + w * 0.5f, cy = y + h * 0.5f;
        float qx = Mathf.Abs(px - cx) - (w * 0.5f - r);
        float qy = Mathf.Abs(py - cy) - (h * 0.5f - r);
        float outside = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude;
        return outside + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
    }

    internal static float Coverage(float distance) => Mathf.Clamp01(0.5f - distance);

    // ---------- icons ----------

    abstract class Mark { public abstract float Distance(Vector2 p); }

    sealed class Stroke : Mark
    {
        readonly List<Vector2> points; readonly float half;
        public Stroke(float width, List<Vector2> points) { this.points = points; half = width * 0.5f; }
        public override float Distance(Vector2 p)
        {
            float best = float.MaxValue;
            for (int i = 0; i < points.Count - 1; i++)
                best = Mathf.Min(best, SegmentDistance(p, points[i], points[i + 1]));
            return best - half;
        }
    }

    sealed class Fill : Mark
    {
        readonly List<Vector2> polygon;
        public Fill(List<Vector2> polygon) { this.polygon = polygon; }
        public override float Distance(Vector2 p)
        {
            float edge = float.MaxValue;
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                Vector2 a = polygon[i], b = polygon[j];
                edge = Mathf.Min(edge, SegmentDistance(p, a, b));
                if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            return inside ? -edge : edge;
        }
    }

    sealed class Disc : Mark
    {
        readonly Vector2 centre; readonly float radius;
        public Disc(float x, float y, float r) { centre = new Vector2(x, y); radius = r; }
        public override float Distance(Vector2 p) => (p - centre).magnitude - radius;
    }

    static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = ab.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0;
        return (p - (a + ab * t)).magnitude;
    }

    static List<Vector2> Pts(params float[] xy)
    {
        var list = new List<Vector2>();
        for (int i = 0; i < xy.Length; i += 2)
            list.Add(new Vector2(xy[i], xy[i + 1]));
        return list;
    }

    // Arc in SVG space (y down), angles in degrees, sweeping from 'from' to 'to'.
    static List<Vector2> Arc(float cx, float cy, float r, float from, float to, List<Vector2> into = null)
    {
        into ??= new List<Vector2>();
        int steps = Mathf.Max(8, Mathf.CeilToInt(Mathf.Abs(to - from) / 6f));
        for (int i = 0; i <= steps; i++)
        {
            float a = Mathf.Lerp(from, to, i / (float)steps) * Mathf.Deg2Rad;
            into.Add(new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r));
        }
        return into;
    }

    static Stroke S(float w, List<Vector2> pts) => new Stroke(w, pts);
    static Stroke S(float w, params float[] xy) => new Stroke(w, Pts(xy));

    static Mark[] Speaker() => new Mark[] { S(2.4f, 4, 9.5f, 8, 9.5f, 13, 5, 13, 19, 8, 14.5f, 4, 14.5f, 4, 9.5f) };
    static Mark[] Note() => new Mark[] { S(2.4f, 9, 18, 9, 6, 19, 4, 19, 16), new Disc(6.5f, 18, 2.9f), new Disc(16.5f, 16, 2.9f) };

    static Mark[] Concat(params Mark[][] parts)
    {
        var all = new List<Mark>();
        foreach (var part in parts) all.AddRange(part);
        return all.ToArray();
    }

    static readonly Dictionary<string, Func<Mark[]>> Icons = new Dictionary<string, Func<Mark[]>>
    {
        ["icon_pause"] = () => new Mark[] { S(3.4f, 9, 5, 9, 19), S(3.4f, 15, 5, 15, 19) },
        ["icon_play"] = () => new Mark[] { new Fill(Pts(7.5f, 4.5f, 19.5f, 12, 7.5f, 19.5f)), S(1.6f, 7.5f, 4.5f, 19.5f, 12, 7.5f, 19.5f, 7.5f, 4.5f) },
        ["icon_home"] = () => new Mark[] { S(2.8f, 4, 11, 12, 4, 20, 11), S(2.8f, 6, 10, 6, 20, 18, 20, 18, 10), S(2.8f, 10, 20, 10, 15, 14, 15, 14, 20) },
        ["icon_restart"] = () => new Mark[] { S(2.8f, Arc(12, 12, 8, 0, 312.5f)), S(2.8f, 20, 4, 20, 9, 15, 9) },
        ["icon_trophy"] = () =>
        {
            var cup = Pts(7, 9, 7, 4, 17, 4, 17, 9);
            Arc(12, 9, 5, 0, 180, cup);
            return new Mark[]
            {
                S(2.6f, cup),
                S(2.6f, Arc(7, 7, 3, 90, 270)),
                S(2.6f, Arc(17, 7, 3, -90, 90)),
                S(2.6f, 12, 14, 12, 18), S(2.6f, 8, 20, 16, 20)
            };
        },
        ["icon_crown"] = () => new Mark[] { new Fill(Pts(3, 8, 7.5f, 12, 12, 5, 16.5f, 12, 21, 8, 19, 19, 5, 19)), S(2.2f, 3, 8, 7.5f, 12, 12, 5, 16.5f, 12, 21, 8, 19, 19, 5, 19, 3, 8) },
        ["icon_sound_on"] = () => Concat(Speaker(), new Mark[] { S(2.4f, Arc(13.85f, 12, 4, -48.5f, 48.5f)), S(2.4f, Arc(13.9f, 12, 7.5f, -47.2f, 47.2f)) }),
        ["icon_sound_off"] = () => Concat(Speaker(), new Mark[] { S(2.4f, 16, 9, 21, 15), S(2.4f, 21, 9, 16, 15) }),
        ["icon_music_on"] = () => Note(),
        ["icon_music_off"] = () => Concat(Note(), new Mark[] { S(2.4f, 3, 3, 21, 21) }),
        ["icon_close"] = () => new Mark[] { S(3.4f, 6, 6, 18, 18), S(3.4f, 18, 6, 6, 18) },
        ["icon_video"] = () =>
        {
            var body = Pts(2.5f, 9, 2.5f, 15);
            Arc(5.5f, 15, 3, 180, 90, body);
            Arc(12.5f, 15, 3, 90, 0, body);
            Arc(12.5f, 9, 3, 0, -90, body);
            Arc(5.5f, 9, 3, -90, -180, body);
            return new Mark[] { S(2.4f, body), new Fill(Pts(15.5f, 10.5f, 21.5f, 7, 21.5f, 17, 15.5f, 13.5f)), S(2.4f, 15.5f, 10.5f, 21.5f, 7, 21.5f, 17, 15.5f, 13.5f, 15.5f, 10.5f) };
        },
        ["icon_user"] = () => new Mark[] { S(2.6f, Arc(12, 8, 4, 0, 360)), S(2.6f, Arc(12, 20, 8, 180, 360)) },
    };

    static void WriteIcon(string name, Func<Mark[]> build)
    {
        Mark[] marks = build();
        float scale = IconSize / 24f;
        var pixels = new Color32[IconSize * IconSize];
        for (int y = 0; y < IconSize; y++)
        {
            for (int x = 0; x < IconSize; x++)
            {
                // Pixel centre in the 24-unit SVG space (y down).
                var p = new Vector2((x + 0.5f) / scale, (IconSize - y - 0.5f) / scale);
                float d = float.MaxValue;
                foreach (Mark mark in marks)
                    d = Mathf.Min(d, mark.Distance(p));
                float alpha = Coverage(d * scale);
                pixels[y * IconSize + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(255 * alpha));
            }
        }
        Save(name, IconSize, IconSize, pixels);
        borders[name] = Vector4.zero;
    }

    // ---------- files ----------

    static readonly Dictionary<string, Vector4> borders = new Dictionary<string, Vector4>();

    internal static void Save(string name, int width, int height, Color32[] pixels)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes($"{Folder}/{name}.png", texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
    }

    static void ConfigureImporters()
    {
        foreach (var pair in borders)
        {
            string path = $"{Folder}/{pair.Key}.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.spriteBorder = pair.Value;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
