using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using In.App.Update;
using In.App.Update.DataModel;
using Newtonsoft.Json;
using Debug = UnityEngine.Debug;

namespace In.App.Update
{
    public class UpdateManager : MonoBehaviour
    {
        public static UpdateManager Instance;
        private string currentVersion = "1.0.0";
        public static string releaseDataFileName = "release_data.json";
        public string releaseDataPath;
        private string localPath = Application.dataPath; // Path to the current executable
        private VersionData downloadedVersion;
        private CancellationTokenSource cts;
        
        private Dictionary<RuntimePlatform, BaseAppUpdater> updaters = new()
        {
            { RuntimePlatform.WindowsPlayer, new WindowsAppUpdater() },
            { RuntimePlatform.OSXPlayer, new MacOSAppUpdater() }
        };

        public async void UpdateBuild()
        {
            string versionString = await File.ReadAllTextAsync(GetReleaseDataPath());
            List<VersionData> versions = JsonConvert.DeserializeObject<List<VersionData>>(versionString);
            VersionData version = versions.OrderByDescending(v => v.versionName).First();
            updaters[Application.platform].StartNewBuild(version);
        }

        private void Awake()
        {
            Instance = this;
        }

        public string GetReleaseDataPath()
        {
            releaseDataPath = Path.Combine(Application.persistentDataPath, releaseDataFileName);
            return releaseDataPath;
        }

        private async UniTask UpdateVersion(VersionData version)
        {
            if (updaters.ContainsKey(Application.platform))
            {
                string path = Path.Combine(Application.persistentDataPath, "update.zip");
                await GoogleDriveFileManager.GetInstance()
                    .DownloadFileAsync(version.fileId, path, onProgress: (progress) => { });
                string extractPath = Path.Combine(Path.GetDirectoryName(path), $"extracted");
                if (!Directory.Exists(extractPath)) Directory.CreateDirectory(extractPath);
                ZipFile.ExtractToDirectory(path, extractPath, true);
                File.Delete(path);
                updaters[Application.platform].StartNewBuild(version);
            }
        }

        public void CancelDownload()
        {
            cts?.Cancel();
        }

        public async UniTask<List<VersionData>> GetAvailableVersions()
        {
            await GoogleDriveFileManager.GetInstance().GetLatestVersions();
            if (!File.Exists(GetReleaseDataPath())) return new List<VersionData>();
            string versionString = await File.ReadAllTextAsync(GetReleaseDataPath());
            List<VersionData> versions = JsonConvert.DeserializeObject<List<VersionData>>(versionString);
            versions = versions.OrderByDescending(v => v.versionName).ToList();
            return versions;
        }

        public async UniTask DownloadVersion(VersionData version, Action<float> onProgress = null,
            Action onComplete = null, Action<string> onFailure = null)
        {
            cts = new CancellationTokenSource();
            string path = Path.Combine(Application.persistentDataPath, "update.zip");
            await GoogleDriveFileManager.GetInstance().DownloadFileAsync(version.fileId, path, cts.Token,
                onProgress: (progress) =>
                {
                    UniTask.Void(async () =>
                    {
                        await UniTask.SwitchToMainThread();
                        onProgress?.Invoke(progress);
                        // Safe update
                    });
                }, onComplete: () =>
                {
                    UniTask.Void(async () =>
                    {
                        await UniTask.SwitchToMainThread();
                        onComplete?.Invoke();

                        // Safe update
                    });
                }, onFailure: (error) =>
                {
                    UniTask.Void(async () =>
                    {
                        await UniTask.SwitchToMainThread();
                        onFailure?.Invoke(error);

                        // Safe update
                    });
                });
            string extractPath = Path.Combine(Path.GetDirectoryName(path), $"extracted");
            if (!Directory.Exists(extractPath)) Directory.CreateDirectory(extractPath);
            ZipFile.ExtractToDirectory(path, extractPath, true);
            File.Delete(path);
            downloadedVersion = version;
        }

        public async UniTask Restart()
        {
            updaters[Application.platform].StartNewBuild(downloadedVersion);
        }

        public async UniTask<bool> IsUpdateAvailable(string versionName)
        {
            int versionCode = GoogleDriveFileManager.GetInstance().GetVersionCode(versionName);
            List<VersionData> versions = await GetAvailableVersions();
            VersionData latestVersion = versions.OrderByDescending(v => v.versionName).First();
            return latestVersion.versionCode > versionCode;
        }

        public async UniTask UpdateToLatestVersion()
        {
            List<VersionData> versions = await GetAvailableVersions();
            VersionData latestVersion = versions.OrderByDescending(v => v.versionName).First();
            if (latestVersion.versionName != currentVersion)
            {
                await UpdateVersion(latestVersion);
            }

        }

    }
}
