using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using System.IO.Compression;
using System.Linq;
using In.App.Update;

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
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Build Path:",GUILayout.Width(80));
        // Button to open folder browser
        if (GUILayout.Button("Browse", GUILayout.Width(70)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Build Folder", buildPath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                buildPath = selectedPath;
            }
        }
        EditorGUILayout.EndHorizontal();
        // Example of displaying the chosen path
        if (!string.IsNullOrEmpty(buildPath))
        {
            EditorGUILayout.HelpBox($"Current Build Path: {buildPath}", MessageType.Info);
        }
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
            string zipFilePath = Path.Combine(buildPath, $"v{versionName}.zip");
            CreateZip(fullBuildPath, zipFilePath);
            Debug.Log($"Build output zipped at: {zipFilePath}");
            //UploadBuild(zipFilePath);
        }
        else
        {
            Debug.LogError($"Build failed with {report.summary.totalErrors} errors.");
        }
    }
    private void CreateZip(string sourceDirectory, string zipFilePath)
    {
        try
        {
            // Ensure the destination ZIP path doesn't exist
            if (File.Exists(zipFilePath))
            {
                File.Delete(zipFilePath);
            }

            // Create a temporary clean directory
            string tempDirectory = Path.Combine(Path.GetTempPath(), "CleanZipTemp");
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, true);
            }
            Directory.CreateDirectory(tempDirectory);

            // Copy only valid files (exclude hidden/system files)
            CopyFilesRecursively(new DirectoryInfo(sourceDirectory), new DirectoryInfo(tempDirectory));

            // Create ZIP from the cleaned directory
            ZipFile.CreateFromDirectory(tempDirectory, zipFilePath);

            // Cleanup temporary directory
            Directory.Delete(tempDirectory, true);

            Debug.Log("ZIP file created successfully at: " + zipFilePath);
        }
        catch (Exception ex)
        {
            Debug.Log("Error creating ZIP file: " + ex.Message);
        }
    }

    private static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
    {
        foreach (DirectoryInfo dir in source.GetDirectories())
        {
            // Skip hidden/system directories
            if ((dir.Attributes & FileAttributes.Hidden) != 0 || (dir.Attributes & FileAttributes.System) != 0)
            {
                continue;
            }

            DirectoryInfo newTarget = target.CreateSubdirectory(dir.Name);
            CopyFilesRecursively(dir, newTarget);
        }

        foreach (FileInfo file in source.GetFiles())
        {
            // Skip hidden/system files
            if ((file.Attributes & FileAttributes.Hidden) != 0 || (file.Attributes & FileAttributes.System) != 0)
            {
                continue;
            }

            file.CopyTo(Path.Combine(target.FullName, file.Name), true);
        }
    }
    public async void UploadBuild(string buildPath)
    {
        Debug.Log($"Uploading build...:{buildPath}");
        Google.Apis.Drive.v3.Data.File file= await GoogleDriveFileManager.GetInstance().UploadFileAsync(buildPath, Application.productName);
        Debug.Log($"Build Uploaded Successfully:{file.Id}");
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
