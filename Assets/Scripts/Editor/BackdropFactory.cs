using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Draws the kitchen backdrop's textures (design canvas: "Backdrop: repeating bands, any width").
/// Band textures tile horizontally, so the backdrop fills any screen width without gaps.
/// </summary>
public static class BackdropFactory
{
    static readonly Color32 WallTop = Hex("F4CBC0"), WallBottom = Hex("FAE1D4");
    static readonly Color32 Trim = Hex("E9A99C"), Cream = Hex("FFF7EC");
    static readonly Color32 TileFace = Hex("FFF4E2"), Grout = Hex("EBD9C2");
    static readonly Color32 WoodLight = Hex("F2B860"), Wood = Hex("E39C45"), WoodDark = Hex("C9803A"), WoodShine = Hex("FFD88A");
    static readonly Color32 Mint = Hex("A8D3CE"), MintDark = Hex("8FBFBA"), MintPanel = Hex("BCDDD9"), Knob = Hex("E8B04E"), KnobShine = Hex("FFE2A0");
    static readonly Color32 Lamp = Hex("FFC94D"), LampLip = Hex("D9A22E"), Bulb = Hex("FFF4C8");

    // name -> (imported as sprite, wrap mode)
    static readonly Dictionary<string, (bool sprite, TextureWrapMode wrap)> imports = new Dictionary<string, (bool, TextureWrapMode)>();

    [MenuItem("Merge&Munch/UI/Generate Backdrop Textures")]
    public static void GenerateAll()
    {
        imports.Clear();
        Wall();
        Wallpaper();
        TrimBand();
        Tiles();
        Counter();
        Cabinet();
        Glow();
        LampShade();
        Sparkle();
        Dash();
        AssetDatabase.Refresh();
        Configure();
        Debug.Log("MM_BACKDROP_TEXTURES_OK");
    }

    public static Texture2D LoadTexture(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{UISpriteFactory.Folder}/{name}.png");
    public static Sprite LoadSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{UISpriteFactory.Folder}/{name}.png");

    // ---------- textures (pixel rows start at the bottom) ----------

    static void Wall()
    {
        const int w = 4, h = 256;
        Fill("bd_wall", w, h, false, TextureWrapMode.Clamp, (x, y) => Color32.Lerp(WallBottom, WallTop, y / (h - 1f)));
    }

    // White dots tinted cream by the RawImage; one tile is 64 canvas units.
    static void Wallpaper()
    {
        const int s = 128;
        Fill("bd_wallpaper", s, s, false, TextureWrapMode.Repeat, (x, y) =>
        {
            float big = Disc(x, y, 32, 96, 10f) * 0.55f;
            float small = Disc(x, y, 96, 32, 6f) * 0.35f;
            return new Color32(255, 255, 255, A(Mathf.Max(big, small)));
        });
    }

    // Scalloped bunting under the picture rail; one tile is 32x22 units.
    static void TrimBand()
    {
        const int w = 64, h = 44;
        Fill("bd_trim", w, h, false, TextureWrapMode.Repeat, (x, y) =>
        {
            float scallop = Disc(x, y, 32, h, 32f);
            float dot = Disc(x, y, 32, h - 20, 6f);
            Color32 c = Color32.Lerp(Trim, Cream, dot);
            c.a = A(scallop);
            return c;
        });
    }

    // Backsplash tile with grout on its top and left edges; one tile is 40 units.
    static void Tiles()
    {
        const int s = 64;
        Fill("bd_tiles", s, s, false, TextureWrapMode.Repeat, (x, y) =>
        {
            bool grout = x < 3 || y >= s - 3;
            bool shine = !grout && x < 8 && y > s - 12;
            return grout ? Grout : shine ? Color32.Lerp(TileFace, Cream, 0.6f) : TileFace;
        });
    }

    static void Counter()
    {
        const int w = 4, h = 64;
        Fill("bd_counter", w, h, false, TextureWrapMode.Clamp, (x, y) =>
        {
            float t = 1f - y / (h - 1f);
            if (y >= h - 4) return WoodShine;
            return t < 0.7f ? Color32.Lerp(WoodLight, Wood, t / 0.7f) : Color32.Lerp(Wood, WoodDark, (t - 0.7f) / 0.3f);
        });
    }

    // One cabinet door; 120 units wide, stretched to the band's height.
    static void Cabinet()
    {
        const int w = 128, h = 320;
        Fill("bd_cabinet", w, h, false, TextureWrapMode.Repeat, (x, y) =>
        {
            if (x >= 6 && x < 9) return MintDark;                       // gap between doors
            if (y < 12) return MintDark;                                // kick plate
            float panel = UISpriteFactory.Coverage(UISpriteFactory.RoundedRectDistance(x + 0.5f, y + 0.5f, 22, 26, 92, h - 64, 12));
            float rim = UISpriteFactory.Coverage(UISpriteFactory.RoundedRectDistance(x + 0.5f, y + 0.5f, 19, 23, 98, h - 58, 14));
            Color32 c = Color32.Lerp(Mint, MintDark, rim);
            c = Color32.Lerp(c, MintPanel, panel);
            float knob = Disc(x, y, 64, h - 44, 8f);
            float shine = Disc(x, y, 62, h - 41, 3f);
            c = Color32.Lerp(c, Knob, knob);
            return Color32.Lerp(c, KnobShine, shine);
        });
    }

    static void Glow()
    {
        const int s = 256;
        Fill("bd_glow", s, s, true, TextureWrapMode.Clamp, (x, y) =>
        {
            float d = new Vector2(x + 0.5f - s / 2f, y + 0.5f - s / 2f).magnitude / (s / 2f);
            float a = Mathf.Clamp01(1f - d);
            return new Color32(255, 255, 255, A(a * a));
        });
    }

    // Pendant lamp: rounded dome with a darker lip and a bulb under it (design 150x92 at 2x).
    static void LampShade()
    {
        const int w = 300, h = 184;
        Fill("bd_lamp", w, h, true, TextureWrapMode.Clamp, (x, y) =>
        {
            float px = x + 0.5f, py = y + 0.5f;
            // Half-ellipse bulb, 140x44, hanging under the dome.
            float bulb = UISpriteFactory.Coverage((new Vector2((px - 150) / 70f, (py - 52) / 44f).magnitude - 1f) * 44f) * (py < 52 ? 1 : 0);
            float lip = Dome(px, py + 12);
            float dome = Dome(px, py);
            Color32 c = Bulb;
            float a = bulb;
            c = Color32.Lerp(c, LampLip, lip); a = Mathf.Max(a, lip);
            c = Color32.Lerp(c, Lamp, dome); a = Mathf.Max(a, dome);
            c.a = A(a);
            return c;
        });
    }

    // Half-ellipse dome standing on y = 52 of the lamp texture (150 x 132 radii).
    static float Dome(float px, float py)
    {
        if (py < 52f)
            return 0f;
        float d = (new Vector2((px - 150f) / 150f, (py - 52f) / 132f).magnitude - 1f) * 132f;
        return UISpriteFactory.Coverage(d);
    }

    static void Sparkle()
    {
        const int s = 64;
        Fill("bd_sparkle", s, s, true, TextureWrapMode.Clamp, (x, y) =>
        {
            // Four-point star: |dx|^0.5 + |dy|^0.5 <= r^0.5
            float dx = Mathf.Abs(x + 0.5f - 32) / 30f, dy = Mathf.Abs(y + 0.5f - 32) / 30f;
            float v = Mathf.Sqrt(dx) + Mathf.Sqrt(dy);
            return new Color32(255, 255, 255, A(Mathf.Clamp01((1f - v) * 12f)));
        });
    }

    // Dash for line renderers (tiled along the line).
    static void Dash()
    {
        const int w = 32, h = 8;
        Fill("bd_dash", w, h, false, TextureWrapMode.Repeat, (x, y) => new Color32(255, 255, 255, (byte)(x < 18 ? 255 : 0)));
    }

    // ---------- helpers ----------

    delegate Color32 Pixel(int x, int y);

    static void Fill(string name, int width, int height, bool sprite, TextureWrapMode wrap, Pixel pixel)
    {
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                pixels[y * width + x] = pixel(x, y);
        UISpriteFactory.Save(name, width, height, pixels);
        imports[name] = (sprite, wrap);
    }

    static float Disc(int x, int y, float cx, float cy, float r) => UISpriteFactory.Coverage(new Vector2(x + 0.5f - cx, y + 0.5f - cy).magnitude - r);

    static byte A(float a) => (byte)Mathf.RoundToInt(255 * Mathf.Clamp01(a));

    static Color32 Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }

    static void Configure()
    {
        foreach (var pair in imports)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath($"{UISpriteFactory.Folder}/{pair.Key}.png");
            importer.textureType = pair.Value.sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            if (pair.Value.sprite)
                importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = pair.Value.wrap;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
