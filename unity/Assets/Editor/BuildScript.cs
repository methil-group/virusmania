using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

public class BuildScript
{
    public static void PerformBuild()
    {
        PlayerSettings.WebGL.template = "PROJECT:VirusMania";
        PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { GraphicsDeviceType.OpenGLES3 });
        
        string buildPath = "Build/WebGL";
        string[] scenes = new string[] {
            "Assets/Scenes/SplashScreen.unity",
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/OnBoarding.unity",
            "Assets/Scenes/Game.unity"
        };

        BuildReport report = BuildPipeline.BuildPlayer(scenes, buildPath, BuildTarget.WebGL, BuildOptions.None);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new System.Exception($"Build WebGL échoué : {report.summary.result}. Voir le rapport Unity pour plus de détails.");
        }

        Debug.Log($"Build WebGL terminé dans {buildPath} ({report.summary.totalSize} octets).");
    }
}
