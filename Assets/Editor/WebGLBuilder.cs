#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BrainCitizen.EditorTools
{
    /// <summary>
    /// One-click WebGL build that places its output where the web/ Express
    /// server can serve it. Triggered from the menu (BrainCitizen → Build
    /// WebGL) or from the command line via -executeMethod
    /// BrainCitizen.EditorTools.WebGLBuilder.BuildBatch.
    ///
    /// Output: <repo-root>/web/public/build/braincitizen/
    /// </summary>
    public static class WebGLBuilder
    {
        // Project root = parent of Assets/. Output sits at web/public/build/braincitizen/.
        const string OutputRelativeToProject = "web/public/build/braincitizen";

        // Always include the Hub first; the rest are optional and only included if they
        // exist on disk. Re-run after adding a new scene.
        static readonly string[] CandidateScenes =
        {
            "Assets/Scenes/HubScene.unity",
            "Assets/Scenes/TrueFalseNews.unity",
            "Assets/Scenes/FlagQuiz.unity",
            "Assets/Scenes/WordSearch.unity",
            "Assets/Scenes/EmotionID.unity",
            "Assets/Scenes/MathSprint.unity",
            "Assets/Scenes/MazeRunner.unity",
            "Assets/Scenes/MemoryMatch.unity",
            "Assets/Scenes/DoppiFacts.unity",
            "Assets/Scenes/TimelineSort.unity",
            "Assets/Scenes/CivicQuiz.unity",
        };

        [MenuItem("BrainCitizen/Build WebGL")]
        public static void Build()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                Debug.Log("[WebGLBuilder] Switching active build target to WebGL...");
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                {
                    Debug.LogError("[WebGLBuilder] Failed to switch to WebGL build target. Install the WebGL module in Unity Hub.");
                    return;
                }
            }

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string outputPath  = Path.GetFullPath(Path.Combine(projectRoot, OutputRelativeToProject));
            Directory.CreateDirectory(outputPath);

            var scenes = CandidateScenes.Where(File.Exists).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("[WebGLBuilder] No scenes found in Assets/Scenes/. Build aborted.");
                return;
            }
            Debug.Log($"[WebGLBuilder] Including {scenes.Length} scene(s): {string.Join(", ", scenes)}");

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.exceptionSupport  = WebGLExceptionSupport.None;
            PlayerSettings.WebGL.dataCaching       = true;
            PlayerSettings.runInBackground         = false;

            var options = new BuildPlayerOptions
            {
                scenes           = scenes,
                locationPathName = outputPath,
                target           = BuildTarget.WebGL,
                options          = BuildOptions.None,
            };

            Debug.Log($"[WebGLBuilder] Building to: {outputPath}");
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[WebGLBuilder] OK — {summary.totalSize / 1024 / 1024} MB, {summary.totalTime.TotalSeconds:F1}s.");
                Debug.Log($"[WebGLBuilder] Open: {outputPath}/index.html  (or run the portal server at /web)");
            }
            else
            {
                Debug.LogError($"[WebGLBuilder] FAILED — {summary.totalErrors} errors. Check the Console.");
            }
        }

        /// <summary>Entry point for batch-mode CLI builds. Quits Unity after.</summary>
        public static void BuildBatch()
        {
            Build();
            EditorApplication.Exit(0);
        }
    }
}
#endif
