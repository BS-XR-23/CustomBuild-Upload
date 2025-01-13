using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyAPIPlugin;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using In.App.Update;
using In.App.Update.DataModel;
using Newtonsoft.Json;
using Debug = UnityEngine.Debug;

public class UpdateManager : MonoBehaviour
{
    public static UpdateManager Instance;
    [SerializeField]
    private string UpdateUrl = "https://brainstationo365-my.sharepoint.com/:u:/g/personal/ezazul_haque_brainstation-23_com/EcZzlgIzXmxJg_jOKYSzugQBbzCd_lPcMkIx0qzLDwwi3w?e=neAgK6";
    private string currentVersion = "1.0.0";
    public static string releaseDataFileName="release_data.json";
    public string releaseDataPath;
    private string localPath = Application.dataPath; // Path to the current executable
    // https://drive.google.com/file/d/1lT8Jn63qc1fknH4i4cM2zNurteQVVNpv/view?usp=sharing
    private Dictionary<RuntimePlatform,BaseAppUpdater> updaters=new ()
    {
        {RuntimePlatform.WindowsPlayer,new WindowsAppUpdater()},
        {RuntimePlatform.OSXPlayer,new MacOSAppUpdater()}
    };

    private void Awake()
    {
        Instance = this;
    }
    public string GetReleaseDataPath()
    {
        releaseDataPath = Path.Combine(Application.persistentDataPath, releaseDataFileName);
        return releaseDataPath;
    }
    public async void UpdateVersion(VersionData version)
    {
        if (updaters.ContainsKey(Application.platform))
        {
            string path = Path.Combine(Application.persistentDataPath, "update.zip"); 
            await GoogleDriveFileManager.GetInstance().DownloadFileAsync(version.fileId, path, (progress) => { });
            string extractPath=Path.Combine(Path.GetDirectoryName(path),$"extracted");
            if (!Directory.Exists(extractPath)) Directory.CreateDirectory(extractPath);
            ZipFile.ExtractToDirectory(path, extractPath,true);
            File.Delete(path);
            updaters[Application.platform].StartNewBuild(version);
        }
    }

}
