using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using System.Linq;

public class BuildManagerWindow : EditorWindow
{
    private string versionName = "1.0.0";
    private string releaseTitle = "Initial Release";
    private string releaseNotes = "Add release notes here...";
    private string buildPath = "Builds";
    private string executableName = "MyGame";

    [MenuItem("Tools/Build Manager")]
    public static void ShowWindow()
    {
        GetWindow<BuildManagerWindow>("Build Manager");
    }

    private void OnGUI()
    {
        GUILayout.Label("Build Configuration", EditorStyles.boldLabel);

        // Input fields
        versionName = EditorGUILayout.TextField("Version Name:", versionName);
        releaseTitle = EditorGUILayout.TextField("Release Title:", releaseTitle);
        releaseNotes = EditorGUILayout.TextArea(releaseNotes, GUILayout.Height(100));
        buildPath = EditorGUILayout.TextField("Build Path:", buildPath);
        executableName = EditorGUILayout.TextField("Executable Name:", executableName);

        // Build Button
        if (GUILayout.Button("Build"))
        {
            PerformBuild();
        }
    }

    private void PerformBuild()
    {
        // Ensure the build path exists
        string fullBuildPath = Path.Combine(buildPath, versionName);
        if (!Directory.Exists(fullBuildPath))
        {
            Directory.CreateDirectory(fullBuildPath);
        }

        // Get enabled scenes
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        // Build options
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(fullBuildPath, executableName),
            target = EditorUserBuildSettings.activeBuildTarget,
            options = BuildOptions.None
        };

        // Perform the build
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);

        // Write metadata
        SaveReleaseMetadata(fullBuildPath);

        // Log the result
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded: {fullBuildPath}");
        }
        else
        {
            Debug.LogError($"Build failed with {report.summary.totalErrors} errors.");
        }
    }

    private void SaveReleaseMetadata(string buildDirectory)
    {
        string metadataPath = Path.Combine(buildDirectory, "release_notes.txt");
        string metadataContent = $"Version: {versionName}\n" +
                                 $"Title: {releaseTitle}\n\n" +
                                 $"{releaseNotes}";

        File.WriteAllText(metadataPath, metadataContent);
        Debug.Log("Release metadata saved.");
    }
}
