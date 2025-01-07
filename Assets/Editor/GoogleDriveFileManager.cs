using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using EasyAPIPlugin;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
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
        public async void Start()
        {
            
            // var data=new Dictionary<string,object>
            // {
            //     {"name","Automated Release Distribution"},
            //     {"mimeType","application/vnd.google-apps.folder"}
            // };
            // await GetAccessToken();
            //Post("https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart",data,_credential.Token.AccessToken,TokenType.Bearer,ContentType.ApplicationJson,RequestType.POST,onSuccess:(data)=>Debug.Log($"Folder created:{data}"),onFailure:(data)=>Debug.Log($"Folder creation failed:{data}"));
            //CreateFolder("test" );
            //await UploadFileAsync(Path.Combine(Application.persistentDataPath,"update.zip"),Application.productName);
        }
        
        public async UniTask<File> UploadFileAsync(string filePath, string folderName)
        {
            await InitializeGoogleDriveService();
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
                var uploadRequest = _driveService.Files.Create(fileMetadata, fileStream, GetMimeType(filePath));
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
                    string credPath =Path.Combine(Application.dataPath, "Editor", "token.json");;
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
        // Method to get the access token from the _credential
        private async UniTask<string> GetAccessToken()
        {
            if (IsTokenExpired())
            {
                // Refresh the token if expired
                Debug.Log("Token expired, refreshing...");
                await _credential.RefreshTokenAsync(CancellationToken.None);
            }
            return _credential.Token.AccessToken;
        }
        private bool IsTokenExpired()
        {
            // Get the current time in UTC
            var currentTime = System.DateTime.UtcNow;

            // Get the token's issued time and expiration time
            var issuedTime = _credential.Token.IssuedUtc;
            var expiresIn = _credential.Token.ExpiresInSeconds.GetValueOrDefault();

            // Calculate the expiration time by adding ExpiresInSeconds to IssuedUtc
            var expirationTime = issuedTime.AddSeconds(expiresIn);
            
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