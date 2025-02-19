using System.Diagnostics;
using System.IO;
using In.App.Update.DataModel;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace In.App.Update
{
    public class MacOSAppUpdater: BaseAppUpdater
    {
        public override void StartNewBuild(VersionData versionData)
        {
            string scriptPath = CreateBashScript(versionData);
            RunBashScript(scriptPath);
        }
        private string CreateBashScript(VersionData versionData)
        {
            string appPath = Application.dataPath; // Gets the path to the Data folder
            appPath = Path.Combine(appPath, "MacOS"); // Navigates to the .app bundle's root
            string exePath=GetExeName(appPath);
            string scriptPath = Path.Combine(Application.persistentDataPath, "launch_app.sh");
            string sourcePath = Path.Combine(Application.persistentDataPath, $"extracted/{versionData.exeName}.app/Contents");
            string destinationPath = Application.dataPath;
            // Write the script to a file
            using (StreamWriter writer = new StreamWriter(scriptPath))
            {
                writer.WriteLine("#!/bin/bash");
                writer.WriteLine($"cp -rf \"{sourcePath}/\" \"{destinationPath}/\"");
                writer.WriteLine($"open \"{exePath}\""); // Launch the app using `open`
                writer.WriteLine($"exit 0"); // Launch the app using `open`

            }
            return scriptPath;
        }
        public void RunBashScript(string scriptPath, string arguments = "")
        {
            if (!File.Exists(scriptPath))
            {
                Debug.LogError($"Script not found at path: {scriptPath}");
                return;
            }
    
            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                FileName = "bash",          // Bash path
                Arguments = $"\"{scriptPath}\" {arguments}", // Script path and arguments
                RedirectStandardOutput = true,  // Capture standard output
                RedirectStandardError = true,   // Capture error output
                UseShellExecute = false,        // Enable redirection
                CreateNoWindow = true           // Do not show a terminal window
            };
    
            try
            {
                // Start the process
                using (Process process = new Process())
                {
                    process.StartInfo = processInfo;
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
    
                    process.WaitForExit();
    
                    // Debug logs for Unity
                    if (!string.IsNullOrEmpty(output))
                        Debug.Log($"Script Output: {output}");
                    if (!string.IsNullOrEmpty(error))
                        Debug.LogError($"Script Error: {error}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Exception: {e.Message}");
            }
            Application.Quit();
        }
        private string GetExeName(string targetPath)
        {
            string[] files = Directory.GetFiles(targetPath,"*", SearchOption.TopDirectoryOnly);
            return files[0];
        }
    }
    
    
}