using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using In.App.Update;
using Debug = UnityEngine.Debug;

public class UpdateManager : MonoBehaviour
{
    [SerializeField]
    private string UpdateUrl = "https://brainstationo365-my.sharepoint.com/:u:/g/personal/ezazul_haque_brainstation-23_com/EcZzlgIzXmxJg_jOKYSzugQBbzCd_lPcMkIx0qzLDwwi3w?e=neAgK6";
    private string currentVersion = "1.0.0";
    private string localPath = Application.dataPath; // Path to the current executable
    // https://drive.google.com/file/d/1lT8Jn63qc1fknH4i4cM2zNurteQVVNpv/view?usp=sharing
    private Dictionary<RuntimePlatform,BaseAppUpdater> updaters=new ()
    {
        {RuntimePlatform.WindowsPlayer,new WindowsAppUpdater()},
        {RuntimePlatform.OSXPlayer,new MacOSAppUpdater()}
    };
    private void Start()
    {
        Debug.Log("Platform: " + Application.platform);
        
    }

    public void CheckForUpdates()
    {
        if (updaters.ContainsKey(Application.platform))
        {
            updaters[Application.platform].UpdateApp();
        }
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

}

[System.Serializable]
public class UpdateMetadata
{
    public string version;
    public string downloadUrl;
}
