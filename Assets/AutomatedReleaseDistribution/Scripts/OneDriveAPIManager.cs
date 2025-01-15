using System.Collections;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace In.App.Update
{
    public class OneDriveAPIManager : MonoBehaviour
    {
        private string clientId = "YOUR_CLIENT_ID";
        private string tenantId = "YOUR_TENANT_ID";
        private string clientSecret = "YOUR_CLIENT_SECRET";
        private string accessToken;
        
        public void Download()
        {
            
        }

        public void Upload()
        {
        }
        public async UniTask<string> UploadFileAsync(byte[] file, string url,UnityAction OnFailure = null)
        {
            WWWForm form = new WWWForm();
            form.AddBinaryData("image", file, "image.png", "image/png");
            form.AddField("activationId", "123456");
            form.AddField("isDevelopment","true");
            UnityWebRequest uploadRequest = UnityWebRequest.Post(url, form);
            var operation = uploadRequest.SendWebRequest();
            await UniTask.WaitUntil(() => operation.isDone);

            if (uploadRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error uploading image: " + uploadRequest.error);
            }
            else
            {
                var jsonResponse = operation.webRequest.downloadHandler.text;
                Debug.Log(jsonResponse);
                Debug.Log("File uploaded successfully!");
                uploadRequest.Dispose();
                return jsonResponse;
            }
            uploadRequest.Dispose();
            return null;
        }
        
        IEnumerator GetAccessToken()
        {
            string tokenUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
            WWWForm form = new WWWForm();
            form.AddField("client_id", clientId);
            form.AddField("scope", "https://graph.microsoft.com/.default");
            form.AddField("client_secret", clientSecret);
            form.AddField("grant_type", "client_credentials");

            UnityWebRequest tokenRequest = UnityWebRequest.Post(tokenUrl, form);
            yield return tokenRequest.SendWebRequest();

            if (tokenRequest.result == UnityWebRequest.Result.Success)
            {
                string responseText = tokenRequest.downloadHandler.text;
                var jsonResponse = JsonUtility.FromJson<GraphApiTokenResponse>(responseText);
                accessToken = jsonResponse.access_token;
                Debug.Log("Access token obtained.");
            }
            else
            {
                Debug.LogError($"Failed to get access token: {tokenRequest.error}");
            }
        }

        [System.Serializable]
        private class GraphApiTokenResponse
        {
            public string token_type;
            public string expires_in;
            public string access_token;
        }
    }
}