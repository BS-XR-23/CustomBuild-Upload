using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using In.App.Update.DataModel;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using File = Google.Apis.Drive.v3.Data.File;

namespace In.App.Update
{
    public class GoogleDriveFileManager
    {
        private string[] _scopes = { DriveService.Scope.DriveFile };
        private UserCredential _credential;
        private string tokenPath => Path.Combine(Application.streamingAssetsPath, "token.json");
        private string _credentialsPath =>
            Path.Combine(Application.streamingAssetsPath,
                "google_api_credentials.json"); // Replace with your JSON credentials file path
        private DriveService _driveService;
        private static GoogleDriveFileManager _instance;
        public static GoogleDriveFileManager GetInstance()
        {
            if (_instance == null) _instance = new GoogleDriveFileManager();
            return _instance;
        }
        
        public async UniTask<File> UploadFileAsync(string filePath, string folderName)
        {
            if(_driveService==null) _driveService= await GetDriveService();
            // Target folder ID
            File folder = await GetFolder(folderName); // Replace with the ID of your target folder
            string folderId = folder.Id;
            
            // File to upload
            string fileName = Path.GetFileName(filePath);
            
            // Create file metadata
            var fileMetadata = new File()
            {
                Name = fileName,
                Parents = new List<string> { folderId } // Add the folder ID as a parent
            };

            // Upload the file
            using (var fileStream = new FileStream(filePath, FileMode.Open))
            {
                long totalBytes = fileStream.Length; // Get the total size of the file
                FilesResource.CreateMediaUpload uploadRequest=_driveService.Files.Create(fileMetadata, fileStream, GetMimeType(filePath));;
                uploadRequest.ProgressChanged += (progress) =>
                {
                    double percentage = (double)progress.BytesSent / totalBytes * 100;
                    Debug.Log($"Upload progress: {percentage:F2}% ({progress.BytesSent}/{totalBytes} bytes)");
                };
                uploadRequest.Fields = "id, name, parents";
                IUploadProgress file =await  uploadRequest.UploadAsync();
                if (file.Status == Google.Apis.Upload.UploadStatus.Completed)
                {
                    var uploadedFile = uploadRequest.ResponseBody;
                    Debug.Log($"File uploaded successfully!");
                    Debug.Log($"File ID: {uploadedFile.Id}");
                    Debug.Log($"File Name: {uploadedFile.Name}");
                    Debug.Log($"Parent Folder ID: {string.Join(", ", uploadedFile.Parents)}");
                    return uploadedFile;
                }
                else
                {
                    Debug.Log($"File upload failed. Status: {file.Status}");
                }
            }

            return null;
        }
        public async UniTask<File> UpdateFileAsync(string filePath,string fileId, string folderName)
        {
            if(_driveService==null) _driveService= await GetDriveService();
            // Target folder ID
            File folder = await GetFolder(folderName); // Replace with the ID of your target folder
            string folderId = folder.Id;
            
            // File to upload
            string fileName = Path.GetFileName(filePath);

            // Create file metadata
            var fileMetadata = new File()
            {
                Name = fileName,
                Parents = new List<string> { folderId } // Add the folder ID as a parent
            };

            // Upload the file
            using (var fileStream = new FileStream(filePath, FileMode.Open))
            {
                long totalBytes = fileStream.Length; // Get the total size of the file
                FilesResource.UpdateMediaUpload uploadRequest = _driveService.Files.Update(new Google.Apis.Drive.v3.Data.File(), fileId,fileStream, GetMimeType(filePath));
                uploadRequest.ProgressChanged += (progress) =>
                {
                    double percentage = (double)progress.BytesSent / totalBytes * 100;
                    Debug.Log($"Update progress: {percentage:F2}% ({progress.BytesSent}/{totalBytes} bytes)");
                };
                uploadRequest.Fields = "id, name, parents";
                IUploadProgress file =await  uploadRequest.UploadAsync();
                if (file.Status == Google.Apis.Upload.UploadStatus.Completed)
                {
                    var uploadedFile = uploadRequest.ResponseBody;
                    Debug.Log($"File uploaded successfully!");
                    Debug.Log($"File ID: {uploadedFile.Id}");
                    Debug.Log($"File Name: {uploadedFile.Name}");
                    Debug.Log($"Parent Folder ID: {string.Join(", ", uploadedFile.Parents)}");
                    return uploadedFile;
                }
                else
                {
                    Debug.Log($"File upload failed. Status: {file.Status},{file.Exception.Message}");
                }
            }

            return null;
        }
        
        public async UniTask DownloadFileAsync(string fileId, string saveToPath,CancellationToken cancellationToken=default, Action<float> onProgress = null,Action onComplete=null,Action<string> onFailure=null)
        {
            if(_driveService==null) _driveService= await GetDriveService();
            try
            {
                long totalBytes = await GetFileSize(fileId);
                var request = _driveService.Files.Get(fileId);
                // Open a stream to save the file
                using (var fileStream = new FileStream(saveToPath, FileMode.Create, FileAccess.Write))
                {
                    // Event handler to track progress
                    request.MediaDownloader.ProgressChanged += progress =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        switch (progress.Status)
                        {
                            case DownloadStatus.Downloading:
                                float percentage = (float)(progress.BytesDownloaded * 100.0 / totalBytes);
                                Debug.Log("Download progress: " + percentage.ToString("F2") + "%");
                                onProgress?.Invoke(percentage);
                                break;

                            case DownloadStatus.Completed:
                                Console.WriteLine("Download complete.");
                                onComplete?.Invoke();
                                onProgress?.Invoke(100.0f);
                                break;

                            case DownloadStatus.Failed:
                                onFailure?.Invoke(progress.Exception.Message);
                                Console.WriteLine("Download failed.");
                                break;
                        }
                    };
                    // Execute the download
                    await request.DownloadAsync(fileStream);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while downloading the file: {ex.Message}");
            }
        }
        public async UniTask<long> GetFileSize(string fileId)
        {
            if(_driveService==null) _driveService= await GetDriveService();
            try
            {
                var request = _driveService.Files.Get(fileId);
                request.Fields = "id, name, size"; // Request file size field
                File file = await request.ExecuteAsync();

                if (file.Size.HasValue)
                {
                    Debug.Log($"File size: {file.Size.Value} bytes");
                    return file.Size.Value;
                }
                else
                {
                    Debug.LogError("File size is unavailable.");
                    return -1;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to get file size: {ex.Message}");
                return -1;
            }
        }
        public async UniTask DeleteFileAsync(string fileId)
        {
            if(_driveService==null) _driveService= await GetDriveService();
            try
            {
                await _driveService.Files.Delete(fileId).ExecuteAsync();
                Debug.Log("File deleted successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"An error occurred while deleting the file: {ex.Message}");
            }
        }

        public async UniTask<List<VersionData>> GetVersions()
        {
            
            string releaseDataPath = Path.Combine(Application.persistentDataPath,UpdateManager.releaseDataFileName );
            if (!System.IO.File.Exists(releaseDataPath))
            {
                Google.Apis.Drive.v3.Data.File file=await GoogleDriveFileManager.GetInstance().GetFileByNameAsync(Path.GetFileName(releaseDataPath),Application.productName);
                if (file == null) return new List<VersionData>();
                await GoogleDriveFileManager.GetInstance().DownloadFileAsync(file.Id, releaseDataPath);
            }
            string releaseDataString = await System.IO.File.ReadAllTextAsync(releaseDataPath);
            List<VersionData> versions = JsonConvert.DeserializeObject<List<VersionData>>(releaseDataString);
            versions=versions.OrderByDescending(v => v.versionName).ToList();
            return versions; 
        }
        public async UniTask<List<VersionData>> GetLatestVersions()
        {
            string releaseDataPath = Path.Combine(Application.persistentDataPath,UpdateManager.releaseDataFileName);
            Google.Apis.Drive.v3.Data.File file=await GoogleDriveFileManager.GetInstance().GetFileByNameAsync(Path.GetFileName(releaseDataPath),Application.productName);
            await GoogleDriveFileManager.GetInstance().DownloadFileAsync(file.Id, releaseDataPath);
            string releaseDataString = await System.IO.File.ReadAllTextAsync(releaseDataPath);
            return JsonConvert.DeserializeObject<List<VersionData>>(releaseDataString); 
        }
        private async UniTask<File> GetFolder(string folderName)
        {
            File folder =await GetFolderIdByName(folderName);
            if (folder==null)
            {
                folder =await CreateFolder(folderName);
            }
            return folder;
        }
        private async UniTask<File> CreateFolder(string folderName)
        {
            File file = new File();
            file.MimeType = "application/vnd.google-apps.folder";
            file.Name = folderName;
            File parentFolder = await GetFolderIdByName("Automated Release Distribution");
            file.Parents=new List<string>{parentFolder.Id};
            FilesResource.CreateRequest createRequest= _driveService.Files.Create(file);
            createRequest.Fields = "id, webViewLink, webContentLink"; // Specify the fields you want to retrieve
            File folder = await createRequest.ExecuteAsync();
            Debug.Log($"Folder created: {folder.Id},{folder.WebViewLink},{folder.WebContentLink}");
            return folder;
        }
        private async UniTask<File> GetFolderIdByName(string folderName)
        {
            if (_driveService == null) _driveService = await GetDriveService(); 
            var request = _driveService.Files.List();
            request.Q = $"mimeType = 'application/vnd.google-apps.folder' and name = '{folderName}' and trashed = false";
            request.Fields = "files(id, name)";
            var result =await request.ExecuteAsync();
            if (result.Files != null && result.Files.Count > 0)
            {
                return result.Files[0]; // Return the first matching folder's ID
            }
            return null; // No matching folder found
        }
        public async UniTask<File> GetFileByNameAsync( string fileName,string folderName)
        {
            if (_driveService == null) _driveService = await GetDriveService(); 
            File folder =await GetFolder(folderName);
            var query = $"'{folder.Id}' in parents and name = '{fileName}'  and trashed = false";
            var request = _driveService.Files.List();
            request.Q = query;
            request.Fields = "files(id, name, mimeType)";
            request.PageSize = 1; // Limit to a single file
            try
            {
                var result = await request.ExecuteAsync();  // Use UniTask's ExecuteAsync for async execution

                if (result.Files != null && result.Files.Count > 0)
                {
                    return result.Files[0]; // Return the first file found
                }
                Debug.Log("File not found.");
                return null;
            }
            catch (Exception ex)
            {
                Debug.Log($"An error occurred: {ex.Message}");
                return null;
            }
        }
        public int GetVersionCode(string version)
        {
            // Split the version into components
            string[] parts = version.Split('.');
            if (parts.Length != 3)
            {
                throw new ArgumentException("Version must be in the format 'major.minor.patch'.");
            }

            // Parse major, minor, and patch as integers
            int major = int.Parse(parts[0]);
            int minor = int.Parse(parts[1]);
            int patch = int.Parse(parts[2]);

            // Calculate version code
            int versionCode = (major * 10000) + (minor * 100) + patch;
            return versionCode;
        }
        private string GetMimeType(string filePath)
        {
            // Simple method to determine MIME type based on file extension
            string extension = Path.GetExtension(filePath).ToLower();
            return extension switch
            {
                ".txt" => "text/plain",
                ".jpg" => "image/jpeg",
                ".png" => "image/png",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".zip" => "application/zip", // Added support for .zip files
                _ => "application/octet-stream", // Default for unknown types
            };
        }
        private async UniTask InitializeGoogleDriveService()
        {
            try
            {
                using (var stream = new FileStream(_credentialsPath, FileMode.Open, FileAccess.Read))
                {
                    string credPath =tokenPath;
                    _credential =await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.Load(stream).Secrets,
                        _scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore(credPath, true));
                    Debug.Log($"access toke:{_credential.Token.AccessToken}");
                }
                // Create Drive API service
                _driveService = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = _credential,
                    ApplicationName = "Automated Release Distribution",
                });
                
                Debug.Log("Google Drive service initialized successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError("Error initializing Google Drive service: " + ex.Message);
            }
        }
        public async UniTask<TokenResponse> RefreshAccessTokenAsync(TokenResponse tokenResponse,ClientSecrets clientSecrets)
        {
            // Build the request payload
            var requestBody = new StringBuilder();
            requestBody.Append($"client_id={clientSecrets.ClientId}");
            requestBody.Append($"&client_secret={clientSecrets.ClientSecret}");
            requestBody.Append($"&refresh_token={tokenResponse.RefreshToken}");
            requestBody.Append("&grant_type=refresh_token");

            using (var request = new UnityWebRequest("https://oauth2.googleapis.com/token", UnityWebRequest.kHttpVerbPOST))
            {
                // Set request headers and body
                byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody.ToString());
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

                // Send the request
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    // Parse the response
                    Debug.Log($"Token Refreshed: {request.downloadHandler.text}");
                    return JsonConvert.DeserializeObject<TokenResponse>(request.downloadHandler.text); // JSON response
                }
                else
                {
                    // Log error details
                    Debug.LogError($"Error refreshing token: {request.error}\n{request.downloadHandler.text}");
                    return null;
                }
            }
        }
        public async UniTask<DriveService> GetDriveService()
        {
            
            string tokenData =JsonCryptoUtility.DecryptJsonFromFile(tokenPath);
            TokenResponse tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(tokenData);
            ClientSecrets clientSecrets = new ClientSecrets
            {
                ClientId = "109410393611-h1qq12qm8ul0mlp0i5vjmcuqcohv5ve2.apps.googleusercontent.com",
                ClientSecret = "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"
            };
            
            tokenResponse =await GetAccessToken(tokenResponse,clientSecrets);
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = clientSecrets
            });

            var credential = new UserCredential(flow, "user", tokenResponse);

            return new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Automated Release Distribution"
            });
        }
        // Method to get the access token from the _credential
        public async UniTask<TokenResponse> GetAccessToken(TokenResponse tokenResponse,ClientSecrets clientSecrets)
        {
            if (IsTokenExpired(tokenResponse))
            {
                // Refresh the token if expired
               Debug.Log("Token expired, refreshing...");
               TokenResponse refreshTokenResponse=await RefreshAccessTokenAsync(tokenResponse,clientSecrets);
               refreshTokenResponse.RefreshToken=tokenResponse.RefreshToken;
               refreshTokenResponse.IssuedUtc=DateTime.UtcNow;
               refreshTokenResponse.Scope = tokenResponse.Scope;
               JsonCryptoUtility.EncryptJsonToFile(tokenPath,JsonConvert.SerializeObject(refreshTokenResponse));
               Debug.Log("Token Saved");
               return refreshTokenResponse;
            }
            else
            {
                Debug.Log("Token does not expire");
            }
            return tokenResponse;
        }
        private bool IsTokenExpired(TokenResponse tokenResponse)
        {
            // Get the current time in UTC
            DateTime currentTime = System.DateTime.UtcNow;
            // Get the token's issued time and expiration time
            DateTime issuedTime = tokenResponse.IssuedUtc;
            var expiresIn = tokenResponse.ExpiresInSeconds.GetValueOrDefault();

            // Calculate the expiration time by adding ExpiresInSeconds to IssuedUtc
            var expirationTime = issuedTime.AddSeconds(expiresIn);
            Debug.Log($"{currentTime},{expirationTime}");
            // Compare the current time with the expiration time
            return currentTime >= expirationTime;
        }
        private async UniTask SendRequest(UnityWebRequest request)
        {
            // Send the request and handle response asynchronously with UniTask
            await request.SendWebRequest().ToUniTask();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"Folder created successfully: {request.downloadHandler.text}");
            }
            else
            {
                Debug.LogError($"Error creating folder: {request.error}");
            }
        }
        
    }
}