using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Playgama WebGL release: only WebGL player settings change; Android/iOS settings are untouched.
/// </summary>
public static class PlaygamaWebBuild
{
    public const string Version = "1.0";
    const string Scene = "Assets/Scenes/SampleScene.unity";
    static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "Playgama"));
    public static string Output => Path.Combine(Root, "WebGL");
    public static string Archive => Path.Combine(Root, $"MergeAndMunch_Playgama_v{Version}.zip");

    [MenuItem("Merge&Munch/Playgama/Configure WebGL")]
    public static void Configure()
    {
        var web = NamedBuildTarget.WebGL;
        PlayerSettings.WebGL.template = "PROJECT:Bridge";
        // No engine splash: the run starts the moment the page has loaded.
        PlayerSettings.SplashScreen.show = false;
        PlayerSettings.SplashScreen.showUnityLogo = false;
        // Playgama 5.1.1: decompression fallback on. Gzip keeps the JS fallback fast on hosts without encoding headers.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        // Off: Unity's asset cache writes to IndexedDB, and Playgama 3.2.1 allows no browser storage besides Bridge.
        PlayerSettings.WebGL.dataCaching = false;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.WebGL.showDiagnostics = false;
        PlayerSettings.WebGL.nameFilesAsHashes = false;
        PlayerSettings.WebGL.initialMemorySize = 64;
        PlayerSettings.WebGL.maximumMemorySize = 1024;
        PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
        PlayerSettings.WebGL.powerPreference = WebGLPowerPreference.HighPerformance;
        PlayerSettings.SetManagedStrippingLevel(web, ManagedStrippingLevel.Medium);
        PlayerSettings.SetIl2CppCodeGeneration(web, Il2CppCodeGeneration.OptimizeSize);
        PlayerSettings.SetIl2CppCompilerConfiguration(web, Il2CppCompilerConfiguration.Master);
        UseAndroidQualityOnWebGL();
        AssetDatabase.SaveAssets();
        Debug.Log("MM_PLAYGAMA_CONFIGURE_OK");
    }

    // Most portal players are on phones: WebGL gets the same quality level the Android build ships with.
    static void UseAndroidQualityOnWebGL()
    {
        var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset").FirstOrDefault();
        if (asset == null)
            return;

        var settings = new SerializedObject(asset);
        SerializedProperty perPlatform = settings.FindProperty("m_PerPlatformDefaultQuality");
        int android = -1;
        SerializedProperty webgl = null;
        for (int i = 0; i < perPlatform.arraySize; i++)
        {
            SerializedProperty entry = perPlatform.GetArrayElementAtIndex(i);
            string platform = entry.FindPropertyRelative("first").stringValue;
            if (platform == "Android")
                android = entry.FindPropertyRelative("second").intValue;
            else if (platform == "WebGL")
                webgl = entry.FindPropertyRelative("second");
        }

        if (android >= 0 && webgl != null && webgl.intValue != android)
        {
            webgl.intValue = android;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    [MenuItem("Merge&Munch/Playgama/Build WebGL + ZIP")]
    public static void Build()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            throw new InvalidOperationException("WebGL Build Support is not installed for this editor.");

        Configure();
        if (Directory.Exists(Output))
            Directory.Delete(Output, true);

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { Scene },
            locationPathName = Output,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None
        });

        var summary = report.summary;
        Debug.Log($"MM_PLAYGAMA_BUILD {summary.result} size={summary.totalSize} errors={summary.totalErrors} warnings={summary.totalWarnings} time={summary.totalTime}");
        if (summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("WebGL build failed: " + summary.result);

        Package();
    }

    // The Bridge/AdMob split lives behind #if UNITY_WEBGL; this compiles the Android and iOS players without switching targets.
    [MenuItem("Merge&Munch/Playgama/Check Mobile Scripts Compile")]
    public static void CheckMobileCompile()
    {
        foreach (var (target, group) in new[] { (BuildTarget.Android, BuildTargetGroup.Android), (BuildTarget.iOS, BuildTargetGroup.iOS) })
        {
            string output = Path.Combine(Path.GetTempPath(), "mm-compile-" + target);
            var result = UnityEditor.Build.Player.PlayerBuildInterface.CompilePlayerScripts(
                new UnityEditor.Build.Player.ScriptCompilationSettings { target = target, group = group }, output);
            bool ok = result.assemblies != null && result.assemblies.Contains("Assembly-CSharp.dll");
            Debug.Log($"MM_MOBILE_COMPILE {target} {(ok ? "OK" : "FAILED")}");
        }
    }

    [MenuItem("Merge&Munch/Playgama/Package ZIP")]
    public static void Package()
    {
        foreach (string required in new[] { "index.html", "playgama-bridge-config.json", "playgama-bridge.js", "playgama-bridge-unity.js" })
        {
            if (!File.Exists(Path.Combine(Output, required)))
                throw new FileNotFoundException("Build is missing " + required);
        }

        string[] bad = Directory.GetFiles(Output, "*", SearchOption.AllDirectories)
            .Select(f => f.Substring(Output.Length + 1))
            .Where(f => f.Any(c => c > 127))
            .ToArray();
        if (bad.Length > 0)
            throw new InvalidOperationException("Non-Latin file names: " + string.Join(", ", bad));

        if (File.Exists(Archive))
            File.Delete(Archive);
        // index.html sits at the archive root (Playgama 1.3.2).
        ZipFile.CreateFromDirectory(Output, Archive, System.IO.Compression.CompressionLevel.Optimal, false);
        Debug.Log($"MM_PLAYGAMA_ZIP_OK {Archive} bytes={new FileInfo(Archive).Length}");
    }
}
