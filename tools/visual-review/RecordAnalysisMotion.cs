using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Core.Analysis;
using Core.Brief;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Capture the actual Game View, including UI, without importing frames into Assets.
public static class RecordAnalysisMotion
{
    public static async Task<object> Run()
    {
        Application.runInBackground = true;
        await SceneManager.LoadSceneAsync("Game");
        await Task.Delay(1200);
        var brief = BriefDatabase.Instance.Database.First(b => b.briefTitle.Contains("Mutate"));
        var briefUi = UnityEngine.Object.FindAnyObjectByType<BriefInterface>();
        briefUi.SetupNewBriefShow(brief);
        await Task.Delay(450);
        briefUi.PutNewBrief();
        await Task.Delay(650);
        Core.Timer.TimerController.Instance.StopTimer();
        var ui = AnalysisInterface.Instance;
        var alternative = BriefDatabase.Instance.Database.First(b => b.briefTitle.Contains("Latency")).wantedItem;
        var exit = ui.transform.Find("InnerPanel/AnalysisWindow/Header/ExitButton").gameObject;
        var capture = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("Unity.Pipeline.Editor.Commands.Capture.CaptureCommands")).First(t => t != null);
        var getTarget = capture.GetMethod("GetGameViewTargetTexture", BindingFlags.Static | BindingFlags.NonPublic);
        var folder = Path.Combine(Application.dataPath, "../Logs", "analysis-motion-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"));
        Directory.CreateDirectory(folder);
        var output = RenderTexture.GetTemporary(1280, 720, 0, RenderTextureFormat.ARGB32);
        var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        var timings = new List<double>();
        var clock = System.Diagnostics.Stopwatch.StartNew();
        bool opened = false, hovered = false, unhovered = false, changed = false, closed = false;
        int lastFrame = -1;
        try
        {
            while (clock.Elapsed.TotalSeconds < 6.2)
            {
                double now = clock.Elapsed.TotalSeconds;
                if (!opened && now > .25) { ui.ShowAnalysis(brief.wantedItem.GetHoldItem()); opened = true; }
                if (!hovered && now > 2) { ExecuteEvents.Execute(exit, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler); hovered = true; }
                if (!unhovered && now > 2.6) { ExecuteEvents.Execute(exit, new PointerEventData(EventSystem.current), ExecuteEvents.pointerExitHandler); unhovered = true; }
                if (!changed && now > 3.25) { ui.ShowAnalysis(alternative.GetHoldItem()); changed = true; }
                if (!closed && now > 5.5) { ui.ClosePanel(); closed = true; }
                if (Time.frameCount != lastFrame && (timings.Count == 0 || now - timings[timings.Count - 1] >= 1d / 25))
                {
                    lastFrame = Time.frameCount;
                    var target = (RenderTexture)getTarget.Invoke(null, new object[] { false });
                    if (target == null) throw new InvalidOperationException("No Game View render target.");
                    var previous = RenderTexture.active;
                    try
                    {
                        if (SystemInfo.graphicsUVStartsAtTop) Graphics.Blit(target, output, new Vector2(1, -1), new Vector2(0, 1));
                        else Graphics.Blit(target, output);
                        RenderTexture.active = output;
                        texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0, false);
                        texture.Apply(false);
                        File.WriteAllBytes(Path.Combine(folder, timings.Count.ToString("D5") + ".jpg"), ImageConversion.EncodeToJPG(texture, 88));
                    }
                    finally { RenderTexture.active = previous; }
                    timings.Add(now);
                }
                await Task.Yield();
            }
        }
        finally { UnityEngine.Object.Destroy(texture); RenderTexture.ReleaseTemporary(output); }
        var lines = new List<string> { "ffconcat version 1.0" };
        for (int i = 0; i < timings.Count; i++)
        {
            lines.Add("file '" + i.ToString("D5") + ".jpg'");
            lines.Add("duration " + (i + 1 < timings.Count ? timings[i + 1] - timings[i] : .04).ToString("F6", CultureInfo.InvariantCulture));
        }
        File.WriteAllLines(Path.Combine(folder, "frames.ffconcat"), lines);
        File.WriteAllText(Path.Combine(Application.dataPath, "../Logs/analysis-motion-folder.txt"), Path.GetFullPath(folder));
        await Task.Delay(500);
        ui.ShowAnalysis(brief.wantedItem.GetHoldItem());
        return new { frames = timings.Count, duration = clock.Elapsed.TotalSeconds, folder = Path.GetFullPath(folder), averageCaptureFps = timings.Count / 6.2 };
    }
}
