using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace In.App.Update
{
    public class WindowsAppUpdater: BaseAppUpdater
    {
        public override void UpdateApp()
        {
            string scriptPath = CreateBatScript();
            RunBatScript(scriptPath);
        }
        private string CreateBatScript()
        {
            string extractedPath = Path.Combine(Application.persistentDataPath, "extracted"); // Path to the extracted files
            string scriptPath = Path.Combine(Application.persistentDataPath, "updater.bat"); // Use .sh for macOS/Linux
            string newExeName=GetExeName(extractedPath);
            string destination=Path.Combine(Application.dataPath,"..");
            string oldExeName=GetExeName(destination);
            
            using (StreamWriter writer = new StreamWriter(scriptPath))
            {
                // Windows Batch Script Example
                writer.WriteLine("@echo on");
                writer.WriteLine($"taskkill /IM \"{oldExeName}\" /F > nul 2>&1"); // Kill the running app
                writer.WriteLine("timeout /t 2 > nul"); // Wait for 2 seconds to ensure it's closed
                writer.WriteLine($"xcopy /Y /E /I \"{extractedPath}\" \"{destination}\""); // Copy files to app folder
                writer.WriteLine($"start \"\" \"{Application.dataPath}\\..\\{newExeName}\""); // Relaunch the app
                writer.WriteLine("exit");
            }
            return scriptPath;
        }
        private void RunBatScript(string scriptPath)
        {
            if (!File.Exists(scriptPath))
            {
                Debug.LogError($"Script not found at path: {scriptPath}");
                return;
            }

            try
            {
                // Run the updater script as a separate process
                Process updaterProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = scriptPath,
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
        }
        private string GetExeName(string extractedPath)
        {
            string[] files = Directory.GetFiles(extractedPath, "*.exe", SearchOption.TopDirectoryOnly);
            string exePath = files.FirstOrDefault(file => !files.Contains("UnityCrashHandler64"));
            string exeName = Path.GetFileName(exePath);
            return exeName;
        }
    }
    
}