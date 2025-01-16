using System.Collections.Generic;
using System.IO;
using In.App.Update;
using In.App.Update.DataModel;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public class UpdateVersionWindow : EditorWindow
{
    private VersionData versionToEdit;

    public static void ShowWindow(VersionData version)
    {
        var window = CreateInstance<UpdateVersionWindow>();
        window.versionToEdit = version;
        window.titleContent = new GUIContent($"Update Version {version.versionName}");
        window.minSize = new Vector2(300, 200);
        window.ShowModalUtility(); // Show as a modal popup
    }

    private void OnGUI()
    {
        if (versionToEdit == null)
        {
            EditorGUILayout.LabelField("No version data to edit!", EditorStyles.boldLabel);
            if (GUILayout.Button("Close"))
                Close();
            return;
        }

        GUILayout.Label("Update Version", EditorStyles.boldLabel);

        EditorGUI.BeginDisabledGroup(true); 
        versionToEdit.versionName = EditorGUILayout.TextField("Version Name", versionToEdit.versionName);
        EditorGUI.EndDisabledGroup();
        
        EditorGUI.BeginChangeCheck();
        versionToEdit.releaseTitle = EditorGUILayout.TextField("Release Title", versionToEdit.releaseTitle);
        versionToEdit.releaseNotes = EditorGUILayout.TextArea(versionToEdit.releaseNotes, GUILayout.Height(80));

        if (EditorGUI.EndChangeCheck())
        {
            // Handle real-time updates or validation if necessary
        }

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save", GUILayout.Height(30)))
        {
            UpdateVersionAsync(versionToEdit);
            Close(); // Close the popup
        }

        if (GUILayout.Button("Cancel", GUILayout.Height(30)))
        {
            Close(); // Close the popup without saving
        }
        GUILayout.EndHorizontal();
    }

    private async void UpdateVersionAsync(VersionData versionData)
    {
        List<VersionData> versionDataList = new List<VersionData>();
        string releaseDataString;
        string releaseDataPath = Path.Combine(Application.persistentDataPath, "release_data.json");
        if (File.Exists(releaseDataPath))
        {
            releaseDataString = await File.ReadAllTextAsync(releaseDataPath);
            versionDataList = JsonConvert.DeserializeObject<List<VersionData>>(releaseDataString);
        }
        int index = versionDataList.FindIndex((item) => item.versionName == versionData.versionName);
        if (index >= 0) versionDataList[index] = versionData;
        else versionDataList.Add(versionData);
        releaseDataString = JsonConvert.SerializeObject(versionDataList);
        await File.WriteAllTextAsync(releaseDataPath, releaseDataString);
        Google.Apis.Drive.v3.Data.File releaseFile = await GoogleDriveFileManager.GetInstance().GetFileByNameAsync("release_data.json", Application.productName);
        await GoogleDriveFileManager.GetInstance().UpdateFileAsync(releaseDataPath, releaseFile.Id, Application.productName);
        Debug.Log($"Version {versionToEdit.versionName} updated.");

    }
}