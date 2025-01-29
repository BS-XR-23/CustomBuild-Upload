using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Google.Apis.Drive.v3.Data;
using In.App.Update;
using In.App.Update.DataModel;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine.WSA;
using Application = UnityEngine.Application;
using CompressionLevel = System.IO.Compression.CompressionLevel;
using Debug = UnityEngine.Debug;
using File = System.IO.File;

namespace In.App.Update
{
    public class BuildManagerWindow : EditorWindow
    {
        private string versionName = "1.0.0";
        private string releaseTitle = "Initial Release";
        private string releaseNotes = "Add release notes here...";
        private string buildPath =Path.Combine(Application.dataPath,"..","Builds");
        private string executableName = "MyGame";
        public static string releaseDataFileName = UpdateManager.releaseDataFileName;
        private string releaseDataPath;
        private int selectedTab = 0; // 0 = Build Manager, 1 = Version Control
        private Vector2 scrollPosition;

        private List<VersionData> versions = new List<VersionData>();

        private static bool isConnected = false; 
        [MenuItem("Tools/Build Manager")]
        public static void ShowWindow()
        {
            GetWindow<BuildManagerWindow>("Build Manager");
        }
        // Validation function for visibility
        [MenuItem("Tools/Connect To Drive", true)] // This determines visibility
        public static bool ValidateConnectToDrive()
        {
            isConnected = GoogleDriveFileManager.GetInstance().IsConnected();
            return !isConnected;
        }
        
        [MenuItem("Tools/Connect To Drive")]
        public async static void ConnectToGoogleDrive()
        {
             await GoogleDriveFileManager.GetInstance().Connect();
            Debug.Log("connected successfully");
        }
        
        [MenuItem("Tools/Disconnect From Drive", true)] // This determines visibility
        public static bool ValidateDisconnectGoogleDrive()
        {
            return isConnected;
        }
        [MenuItem("Tools/Disconnect From Drive")]
        public static void DisconnectGoogleDrive()
        {
            GoogleDriveFileManager.GetInstance().Disconnect();
            isConnected = false;
            Debug.Log("disconnected successfully");
        }
        [MenuItem("Tools/RunScript")]
        public static void RunScript()
        {
            string scriptPath = Path.Combine(Application.dataPath, "AutomatedReleaseDistribution", "Editor", "test.sh");
            try
            {
                Process process = new Process();
                string escapedPath = scriptPath;
                // Set the process start info for macOS
                process.StartInfo.FileName = "osascript";
                process.StartInfo.Arguments = $"-e 'tell application \"Terminal\" to do script \"bash \\\"{escapedPath}\\\"\"'";
                Debug.Log($"-e 'tell application \"Terminal\" to do script \"bash \\\"{escapedPath}\\\"\"'");
                // UseShellExecute is true to allow the GUI terminal to open
                process.StartInfo.UseShellExecute = true;

                // Do NOT redirect IO streams when UseShellExecute is true
                process.StartInfo.RedirectStandardOutput = false;
                process.StartInfo.RedirectStandardError = false;
                process.StartInfo.RedirectStandardInput = false;

                // Start the process
                process.Start();
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError("Exception: " + e.Message);
            }
        }

       
        private async void OnEnable()
        {
            releaseDataPath = Path.Combine(Application.persistentDataPath, releaseDataFileName);
            versionName = PlayerSettings.bundleVersion;
            executableName = PlayerSettings.productName;
            GoogleDriveFileManager.GetInstance().GetLatestVersions();
            Debug.Log("on enabled called");
            await LoadVersions();
        }

        private async UniTask LoadVersions()
        {
            try
            {
                versions = await GoogleDriveFileManager.GetInstance().GetVersions();
                //Repaint(); // Force a repaint to update the UI
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load versions: {ex.Message}");
            }
        }

        private void OnGUI()
        {
            // Tab Selection
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Toggle(selectedTab == 0, "Build Manager", EditorStyles.toolbarButton))
                selectedTab = 0;
            if (GUILayout.Toggle(selectedTab == 1, "Version Control", EditorStyles.toolbarButton))
                selectedTab = 1;
            GUILayout.EndHorizontal();
            // Tab Content
            switch (selectedTab)
            {
                case 0:
                    DrawBuildManager();
                    break;
                case 1:
                    DrawVersionControl();
                    break;
            }

        }

        private void DrawVersionControl()
        {
            Debug.Log("versions:" + versions.Count);
            GUILayout.Label("Version Control", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            int indexToRemove = -1; // Store the index of the item to remove
            for (int i = 0; i < versions.Count; i++)
            {
                var version = versions[i];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Accordion Header
                EditorGUILayout.BeginHorizontal();
                version.isExpanded = EditorGUILayout.Foldout(version.isExpanded,
                    $"v{version.versionName} - {version.releaseTitle}", true);
                if (GUILayout.Button("Download", GUILayout.Width(70)))
                {
                    ShowDownloadDialog(version);
                    Debug.Log($"Download version {version.versionName}");
                    // Implement download logic

                }

                EditorGUILayout.EndHorizontal();

                // Accordion Content
                if (version.isExpanded)
                {
                    EditorGUILayout.LabelField("Release Notes:", version.releaseNotes, EditorStyles.wordWrappedLabel);

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Update"))
                    {
                        Debug.Log($"Update version {version.versionName}");
                        UpdateVersionWindow.ShowWindow(version);
                        // Implement update logic


                    }

                    if (GUILayout.Button("Delete"))
                    {
                        if (EditorUtility.DisplayDialog("Confirm Delete",
                                $"Are you sure you want to delete version {version.versionName}?", "Yes", "No"))
                        {
                            OnVersionDeleted(version);
                            indexToRemove = i; // Mark this item for removal
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
            // Remove the marked item after the loop to avoid layout issues
            if (indexToRemove >= 0)
            {
                versions.RemoveAt(indexToRemove);
            }
        }

        private async void ShowDownloadDialog(VersionData version)
        {
            string path = EditorUtility.SaveFilePanel(
                "Choose Download Location",
                "",
                $"v{version.versionName}.zip", // Default file name
                "zip" // Default extension
            );
            if (!string.IsNullOrEmpty(path))
            {
                Debug.Log($"Download started for {version.versionName} to {path}");
                await GoogleDriveFileManager.GetInstance().DownloadFileAsync(version.fileId, path, onProgress:(progress) => { });
                string extractPath = Path.Combine(Path.GetDirectoryName(path), $"v{version.versionName}");
                if (!Directory.Exists(extractPath)) Directory.CreateDirectory(extractPath);
                ZipFile.ExtractToDirectory(path, extractPath, true);
                MakeExecutable(Path.Combine(extractPath, $"{version.exeName}.app", "Contents", "MacOS"));
                //File.Delete(path);
                // Simulate file download - Replace this with your actual download logic
            }
            else
            {
                Debug.Log("Download canceled.");
            }
        }

        private async void OnVersionDeleted(VersionData version)
        {
            await GoogleDriveFileManager.GetInstance().DeleteFileAsync(version.fileId);
            string releaseDataString;
            List<VersionData> versionDataList = new List<VersionData>();
            if (File.Exists(releaseDataPath))
            {
                releaseDataString = await File.ReadAllTextAsync(releaseDataPath);
                versionDataList = JsonConvert.DeserializeObject<List<VersionData>>(releaseDataString);
            }

            int index = versionDataList.FindIndex((item) => item.versionName == version.versionName);
            if (index >= 0) versionDataList.RemoveAt(index);
            releaseDataString = JsonConvert.SerializeObject(versionDataList);
            await File.WriteAllTextAsync(releaseDataPath, releaseDataString);
            Google.Apis.Drive.v3.Data.File releaseFile = await GoogleDriveFileManager.GetInstance()
                .GetFileByNameAsync(releaseDataFileName, Application.productName);
            await GoogleDriveFileManager.GetInstance()
                .UpdateFileAsync(releaseDataPath, releaseFile.Id, Application.productName);
            Debug.Log($"Version {version.versionName} has been deleted.");
            // Implement additional actions, such as logging, notifications, etc.
        }

        private async void DrawBuildManager()
        {
            GUILayout.Label("Build Configuration", EditorStyles.boldLabel);

            // Input fields
            versionName = EditorGUILayout.TextField("Version Name:", versionName);
            if (!IsValidVersion(versionName))
            {
                EditorGUILayout.HelpBox("Version Name must be in the format 'major.minor.patch' (e.g., 0.1.1).",
                    MessageType.Error);
            }

            releaseTitle = EditorGUILayout.TextField("Release Title:", releaseTitle);
            releaseNotes = EditorGUILayout.TextArea(releaseNotes, GUILayout.Height(100));
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Build Path:", GUILayout.Width(80));
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
            EditorGUI.BeginDisabledGroup(!IsValidVersion(versionName));
            if (GUILayout.Button("Build And Publish"))
            {
                PerformBuild();
            }

            EditorGUI.EndDisabledGroup();


        }

        // Validation Method for Version Name
        private bool IsValidVersion(string version)
        {
            // Match format: major.minor.patch where each segment is a number
            return System.Text.RegularExpressions.Regex.IsMatch(version, @"^\d+\.\d+\.\d+$");
        }

        private async void PerformBuild()
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

            string exePath =EditorUserBuildSettings.activeBuildTarget==BuildTarget.StandaloneOSX? Path.Combine(fullBuildPath, executableName):Path.Combine(fullBuildPath, $"{executableName}.exe");
            // Build options
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName =exePath,
                target = EditorUserBuildSettings.activeBuildTarget,
                options = BuildOptions.None
            };

            // Perform the build
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);

            // Log the result
            if (report.summary.result == BuildResult.Succeeded)
            {
               
                Debug.Log($"Build succeeded: {fullBuildPath}");
                PlayerSettings.bundleVersion = versionName;
                string zipFilePath = Path.Combine(buildPath, $"v{versionName}.zip");
                //await CreateZip(fullBuildPath, zipFilePath);
                Debug.Log($"Build output zipped at: {zipFilePath}");
                UploadBuildFromTerminal(fullBuildPath,zipFilePath);
            }
            else
            {
                Debug.LogError($"Build failed with {report.summary.totalErrors} errors.");
            }
        }

        public async void UploadBuild(string buildFolder,string buildPath)
        {
            Debug.Log($"Uploading build...:{buildPath}");
            string fileName = Path.GetFileName(buildPath);
            Google.Apis.Drive.v3.Data.File existingBuildFile = await GoogleDriveFileManager.GetInstance()
                .GetFileByNameAsync(fileName, Application.productName);
            Google.Apis.Drive.v3.Data.File buildFile;
            if (existingBuildFile == null)
                buildFile = await GoogleDriveFileManager.GetInstance()
                    .UploadFileAsync(buildPath, Application.productName);
            else
                buildFile = await GoogleDriveFileManager.GetInstance()
                    .UpdateFileAsync(buildPath, existingBuildFile.Id, Application.productName);
            Debug.Log($"Build Uploaded Successfully:{buildFile.Id}");
            VersionData versionData = new VersionData()
            {
                versionName = versionName,
                versionCode = GoogleDriveFileManager.GetInstance().GetVersionCode(versionName),
                releaseTitle = releaseTitle,
                releaseNotes = releaseNotes,
                exeName = executableName,
                fileId = buildFile.Id
            };
            string releaseDataString;
            List<VersionData> versionDataList = new List<VersionData>();
            Google.Apis.Drive.v3.Data.File existingFile = null;

            existingFile = await GoogleDriveFileManager.GetInstance()
                .GetFileByNameAsync(releaseDataFileName, Application.productName);
            if (existingFile != null)
                await GoogleDriveFileManager.GetInstance()
                    .DownloadFileAsync(existingFile.Id, releaseDataPath, onProgress:(progress) => { });

            if (File.Exists(releaseDataPath))
            {
                releaseDataString = await File.ReadAllTextAsync(releaseDataPath);
                versionDataList = JsonConvert.DeserializeObject<List<VersionData>>(releaseDataString);
            }

            int index = versionDataList.FindIndex((item) => item.versionName == versionName);
            if (index >= 0) versionDataList[index] = versionData;
            else versionDataList.Add(versionData);
            releaseDataString = JsonConvert.SerializeObject(versionDataList);
            await File.WriteAllTextAsync(releaseDataPath, releaseDataString);
            Google.Apis.Drive.v3.Data.File releaseFile = null;
            if (existingFile == null)
                releaseFile = await GoogleDriveFileManager.GetInstance()
                    .UploadFileAsync(releaseDataPath, Application.productName);
            else
                releaseFile = await GoogleDriveFileManager.GetInstance()
                    .UpdateFileAsync(releaseDataPath, existingFile.Id, Application.productName);
        }
        public async void UploadBuildFromTerminal(string buildFolder,string buildPath)
        {
            Debug.Log($"Uploading build...:{buildPath}");
            await GoogleDriveFileManager.GetInstance().GetDriveService(); 
            string fileName = Path.GetFileName(buildPath);
            VersionData versionData = new VersionData()
            {
                versionName = versionName,
                versionCode = GoogleDriveFileManager.GetInstance().GetVersionCode(versionName),
                releaseTitle = releaseTitle,
                releaseNotes = releaseNotes,
                exeName = executableName,
                fileId ="[file_id]"
            };
            string releaseDataString;
            List<VersionData> versionDataList = new List<VersionData>();
            Google.Apis.Drive.v3.Data.File existingFile = null;
            
            existingFile = await GoogleDriveFileManager.GetInstance()
                .GetFileByNameAsync(releaseDataFileName, Application.productName);
            if (existingFile != null)
                await GoogleDriveFileManager.GetInstance()
                    .DownloadFileAsync(existingFile.Id, releaseDataPath, onProgress:(progress) => { });

            if (File.Exists(releaseDataPath))
            {
                releaseDataString = await File.ReadAllTextAsync(releaseDataPath);
                versionDataList = JsonConvert.DeserializeObject<List<VersionData>>(releaseDataString);
            }
           
            int index = versionDataList.FindIndex((item) => item.versionName == versionName);
            if (index >= 0) versionDataList[index] = versionData;
            else versionDataList.Add(versionData);
            
            string tempReleaseDataPath = Path.Combine(Application.dataPath,"AutomatedReleaseDistribution", "Editor", releaseDataFileName);
            releaseDataString = JsonConvert.SerializeObject(versionDataList);
            await File.WriteAllTextAsync(tempReleaseDataPath, releaseDataString);
          
            Google.Apis.Drive.v3.Data.File parentFolder=await GoogleDriveFileManager.GetInstance().GetFolder(Application.productName);
            if(parentFolder!=null) RunBatFile(buildFolder,buildPath,tempReleaseDataPath,parentFolder.Id);
            Debug.Log($"Build Preprocessed Successfully:{buildPath}");
            Debug.Log($"Check Terminal To See The Progress");

        }

        private async static void RunBatFile(string buildFolder,string buildPath,string releaseDataPath,string folderId)
        {
            string accessToken = GoogleDriveFileManager.GetInstance().GetLatestToken();
            string executableName = EditorUserBuildSettings.activeBuildTarget==BuildTarget.StandaloneOSX?"uploader.sh":"uploader.bat";
            string batFilePath = Path.Combine(Application.dataPath, "AutomatedReleaseDistribution", "Editor",executableName);
            string batFileText=await File.ReadAllTextAsync(batFilePath);
            batFileText = batFileText.Replace("[access_token]",accessToken );
            batFileText = batFileText.Replace("[folder_id]",folderId );
            batFileText = batFileText.Replace("[build_path]",buildPath );
            batFileText = batFileText.Replace("[build_folder]",buildFolder );
            batFileText = batFileText.Replace("[release_data_path]",releaseDataPath );
            string updateBatFilePath = Path.Combine(Application.persistentDataPath, executableName);
            await File.WriteAllTextAsync(updateBatFilePath,batFileText);
            if (EditorUserBuildSettings.activeBuildTarget==BuildTarget.StandaloneOSX)
            {
                try
                {
                    Process process = Process.Start(new ProcessStartInfo
                    {
                        FileName = "osascript",          // Bash path
                        Arguments = $"-e 'tell application \"Terminal\" to do script \"bash \\\"{updateBatFilePath}\\\"\"'", // Script path and arguments
                        UseShellExecute = true,// Enable redirection
                        CreateNoWindow = false// Do not show a terminal window
                    });
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Exception: {e.Message}");
                }
                
            }
            else
            {
                Process updaterProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = updateBatFilePath,
                    UseShellExecute = true,
                    CreateNoWindow = false
                });
                // Wait for the updater process to exit before quitting the app
                //updaterProcess.WaitForExit();
            }
           
        }
        

        private async UniTask CreateZip(string sourceDirectory, string zipFilePath)
        {
            try
            {
                // Ensure the destination ZIP path doesn't exist
                if (File.Exists(zipFilePath))
                {
                    File.Delete(zipFilePath);
                }

                await UniTask.Run(() =>
                {
                    ZipFile.CreateFromDirectory(sourceDirectory, zipFilePath);
                    ZipFile.ExtractToDirectory(zipFilePath,
                        Path.Combine(Application.dataPath, Path.Combine("..", "TestBuild")), true);
                    //MakeExecutable(Path.Combine(Application.dataPath, "..", "TestBuild", "MyGame.app", "Contents","MacOS"));
                    Debug.Log("ZIP file created successfully at: " + zipFilePath);
                });
            }
            catch (Exception ex)
            {
                Debug.Log("Error creating ZIP file: " + ex.Message);
            }
        }


        public void MakeExecutable(string directoryPath)
        {
            try
            {
                // Ensure the directory exists
                if (!Directory.Exists(directoryPath))
                {
                    Debug.LogError($"Directory not found: {directoryPath}");
                    return;
                }

                // Use Process to call chmod recursively
                Process chmodProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = $"-c \"find \\\"{directoryPath}\\\" -type f -exec chmod +x {{}} \\;\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                chmodProcess.Start();
                string output = chmodProcess.StandardOutput.ReadToEnd();
                string error = chmodProcess.StandardError.ReadToEnd();
                chmodProcess.WaitForExit();

                // Log results
                if (!string.IsNullOrEmpty(output))
                {
                    Debug.Log($"chmod output: {output}");
                }

                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError($"chmod error: {error}");
                }
                else
                {
                    Debug.Log($"Successfully made all files executable in: {directoryPath}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error making files executable: {ex.Message}");
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
}
