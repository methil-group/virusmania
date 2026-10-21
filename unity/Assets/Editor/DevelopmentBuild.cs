using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class DevelopmentBuild
{
    [MenuItem("VirusMania/Build Windows Preview")]
    public static void PerformBuild()
    {
        var output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Build/Dev/VirusMania.exe"));
        var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        var report = BuildPipeline.BuildPlayer(scenes, output, BuildTarget.StandaloneWindows64,
            BuildOptions.Development | BuildOptions.DetailedBuildReport);
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Windows preview build failed: " + report.summary.result);
        Debug.Log("Windows preview ready: " + output + " (" + report.summary.totalSize + " bytes)");
    }
}
