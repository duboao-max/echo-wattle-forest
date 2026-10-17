using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// BuildWebGL - 打包 WebGL 版本,用于上传 itch.io。
/// 产出到 Builds/WebGL/ 目录,可直接压缩上传。
/// 配置:1080x1920 竖屏(移动端优先)、关闭 Development、GZip 压缩。
/// </summary>
public static class BuildWebGL
{
    [MenuItem("Echo/Build WebGL (itch.io)")]
    public static void Run()
    {
        // 1. 确保场景已保存
        if (EditorSceneManager_HasUnsaved())
        {
            Debug.LogWarning("[BuildWebGL] 场景有未保存改动,先保存");
        }
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        // 2. 输出目录
        string outDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds", "WebGL");
        if (Directory.Exists(outDir))
            Directory.Delete(outDir, true);
        Directory.CreateDirectory(outDir);

        // 3. 场景列表(用 EditorBuildSettings 里已有的)
        string[] scenes = new string[EditorBuildSettings.scenes.Length];
        for (int i = 0; i < scenes.Length; i++)
            scenes[i] = EditorBuildSettings.scenes[i].path;

        // 4. 配置 PlayerSettings(只设置稳定核心项,避免版本 API 差异导致编译失败)
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.WebGL, ScriptingImplementation.IL2CPP);
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.runInBackground = false;
        PlayerSettings.productName = "Echo of the Wattle Forest";

        // 竖屏 1080x1920(移动端)
        PlayerSettings.defaultScreenWidth = 1080;
        PlayerSettings.defaultScreenHeight = 1920;
        PlayerSettings.defaultIsNativeResolution = false;

        // 5. 构建选项
        BuildPlayerOptions opts = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(outDir, "index"),
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        // 6. 执行构建
        BuildReport report = BuildPipeline.BuildPlayer(opts);

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("[BuildWebGL] ✅ 打包成功!输出目录: " + outDir);
            Debug.Log("[BuildWebGL] 总大小: " + (report.summary.totalSize / (1024 * 1024)) + " MB");
            Debug.Log("[BuildWebGL] 下一步:压缩 " + outDir + " 整个文件夹为 zip,上传到 itch.io");
        }
        else
        {
            Debug.LogError("[BuildWebGL] ❌ 打包失败: " + report.summary.result);
            Debug.LogError("[BuildWebGL] 错误数: " + report.summary.totalErrors);
        }
    }

    private static bool EditorSceneManager_HasUnsaved()
    {
        for (int i = 0; i < UnityEditor.SceneManagement.EditorSceneManager.sceneCount; i++)
        {
            var s = UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(i);
            if (s.isDirty) return true;
        }
        return false;
    }
}
