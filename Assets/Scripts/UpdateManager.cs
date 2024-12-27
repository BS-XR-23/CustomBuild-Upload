using System;
using UnityEngine;
using UnityEngine.Networking;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;

public class UpdateManager : MonoBehaviour
{
    [SerializeField]
    private string UpdateUrl = "https://brainstationo365-my.sharepoint.com/:u:/g/personal/ezazul_haque_brainstation-23_com/EcZzlgIzXmxJg_jOKYSzugQBbzCd_lPcMkIx0qzLDwwi3w?e=neAgK6";
    private string currentVersion = "1.0.0";
    private string localPath = Application.dataPath; // Path to the current executable
    // https://drive.google.com/file/d/1lT8Jn63qc1fknH4i4cM2zNurteQVVNpv/view?usp=sharing
    public async void CheckForUpdates()
    {
        // Debug.Log("Checking for updates...");
        // await DownloadAndUpdate(UpdateUrl);
        // Debug.Log("file downloaded");
        Debug.Log($"localPath{localPath},application name:{Application.persistentDataPath},{Application.productName}");
        // ReplaceFiles2(Path.Combine(Application.persistentDataPath, "extracted/test.app/Contents"));
        // RestartApplication2();
        ReplaceFilesWithUpdater(Path.Combine(Application.persistentDataPath, "extracted"));
    }
    //private async Task DownloadAndUpdate(string downloadUrl)
    //{
    //    Debug.Log("download: " + downloadUrl);
    //    string tempPath = Path.Combine(Application.persistentDataPath, "update.zip");

    //    byte[] data=await EasyAPI.DownloadFile(UpdateUrl,onSuccess:(data)=>Debug.Log($"Downloaded:{data}"));
    //    File.WriteAllBytes(tempPath,data);
        
    //    Debug.Log("Update downloaded. Extracting...");
    //    string extractedPath = Path.Combine(Application.persistentDataPath, "extracted");
            
    //    if (Directory.Exists(extractedPath))
    //        Directory.Delete(extractedPath, true);
            
    //    ZipFile.ExtractToDirectory(tempPath, extractedPath);

    //    Debug.Log("Update extracted. Replacing files...");
    //    ReplaceFiles(extractedPath);
    //    // Debug.Log("Restarting application...");
    //    RestartApplication();
    //}

    private void ReplaceFiles(string extractedPath)
    {
        string appFolder = Path.GetDirectoryName(localPath);

        foreach (var file in Directory.GetFiles(extractedPath, "*", SearchOption.AllDirectories))
        {
            string relativePath = file.Substring(extractedPath.Length + 1);
            string destinationPath = Path.Combine(appFolder, relativePath);

            if (File.Exists(destinationPath))
                File.Delete(destinationPath);

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
            File.Copy(file, destinationPath);
        }
    }
    private void ReplaceFilesWithUpdater(string extractedPath)
    {
        string updaterScriptPath = Path.Combine(Application.temporaryCachePath, "updater.bat"); // Use .sh for macOS/Linux
        string[] files = Directory.GetFiles(extractedPath, "*.exe", SearchOption.TopDirectoryOnly);
        string exePath = files.FirstOrDefault(file => !files.Contains("UnityCrashHandler64"));
        string exeName=Path.GetFileName(exePath);
        string destination=Path.Combine(Application.dataPath,"..");
       
        //string exePath2 = System.Reflection.Assembly.GetEntryAssembly().Location;
        //string exeName2 = Path.GetFileName(exePath2); // Get the filename (e.g., "MyApp.exe")
        Debug.Log($"Exe Path:{exeName}");
        // Create the updater script
        using (StreamWriter writer = new StreamWriter(updaterScriptPath))
        {
            // Windows Batch Script Example
            writer.WriteLine("@echo on");
            writer.WriteLine($"taskkill /IM \"{exeName}\" /F > nul 2>&1"); // Kill the running app
            writer.WriteLine("timeout /t 2 > nul"); // Wait for 2 seconds to ensure it's closed
            writer.WriteLine($"xcopy /Y /E /I \"{extractedPath}\" \"{destination}\""); // Copy files to app folder
            writer.WriteLine($"start \"\" \"{Application.dataPath}\\..\\{exeName}\""); // Relaunch the app
            writer.WriteLine("exit");
        }
        try
        {
            // Run the updater script as a separate process
            Process updaterProcess = Process.Start(new ProcessStartInfo
            {
                FileName = updaterScriptPath,
                UseShellExecute = true,
                CreateNoWindow = false
            });

            // Wait for the updater process to exit before quitting the app
            updaterProcess.WaitForExit();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception: {e.Message}");
        }
        // Quit the application after the update process finishes
        //Application.Quit();
    }


    private void RestartApplication()
    {
        string executablePath = Path.Combine(localPath, "MyGame.exe");
        Process.Start(executablePath);
        Application.Quit();
    }
    private void ReplaceFiles2(string extractedPath)
    {
        string appFolder = Path.Combine(localPath);
        string destinationPath = Path.Combine(appFolder);
        
        CopyAllFiles(extractedPath, destinationPath);
    }

    private async void RestartApplication2()
    {
        string appPath = Application.dataPath; // Gets the path to the Data folder
        appPath = Path.Combine(appPath, "MacOS/CustomBuild_Upload"); // Navigates to the .app bundle's root
        appPath = Path.GetFullPath(appPath); // Normalizes the path
        //appPath = Path.Combine(Application.persistentDataPath, "extracted/test.app/Contents/MacOS/CustomBuild_Upload"); // Appends the executable path
        
        // Dynamically generate a Bash script
        string scriptPath = Path.Combine(Application.persistentDataPath, "launch_app.sh");

        // Write the script to a file
        using (StreamWriter writer = new StreamWriter(scriptPath))
        {
            writer.WriteLine("#!/bin/bash");
            writer.WriteLine($"open \"{appPath}\""); // Launch the app using `open`
        }
        
        // Make the script executable
        var chmodProcess = new ProcessStartInfo
        {
            FileName = "chmod",
            Arguments = $"+x \"{scriptPath}\""
        };
        Process.Start(chmodProcess).WaitForExit(); // Ensure the script is executable
       
        // Start a new instance of the application
        // Start the Bash script
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "bash",
            Arguments = scriptPath
        };
        RunScript(scriptPath);
    
    }
    public void RunScript(string scriptPath, string arguments = "")
    {
        // Validate the script file
        if (!File.Exists(scriptPath))
        {
            Debug.LogError($"Script not found at path: {scriptPath}");
            return;
        }

        // Ensure the script is executable
        MakeExecutable(scriptPath);
        // Configure the process
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

                // Read output and errors
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();
                Debug.Log("i am called:"+scriptPath);

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

    private void MakeExecutable(string filePath)
    {
        try
        {
            Process chmodProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"chmod +x '{filePath}'\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            chmodProcess.Start();
            chmodProcess.WaitForExit();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to make script executable: {e.Message}");
        }
    }


    public  void CopyAllFiles(string sourcePath, string destinationPath)
    {
        // Ensure the source directory exists
        if (!Directory.Exists(sourcePath))
        {
            Debug.LogError($"Source directory does not exist: {sourcePath}");
            return;
        }

        // Create the destination directory if it doesn't exist
        if (!Directory.Exists(destinationPath))
        {
            Directory.CreateDirectory(destinationPath);
        }

        // Copy all files in the current directory
        foreach (string filePath in Directory.GetFiles(sourcePath))
        {
            string fileName = Path.GetFileName(filePath);
            string destFilePath = Path.Combine(destinationPath, fileName);
            File.Copy(filePath, destFilePath, true); // Overwrite if the file already exists
        }
        // Recursively copy all subdirectories
        foreach (string subDirPath in Directory.GetDirectories(sourcePath))
        {
            string dirName = Path.GetFileName(subDirPath);
            string destSubDirPath = Path.Combine(destinationPath, dirName);
            CopyAllFiles(subDirPath, destSubDirPath);
        }
    }
}

[System.Serializable]
public class UpdateMetadata
{
    public string version;
    public string downloadUrl;
}
