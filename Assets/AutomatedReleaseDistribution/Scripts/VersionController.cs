using System;
using System.Collections.Generic;
using System.IO;
using In.App.Update.DataModel;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UIElements;

namespace In.App.Update
{
    public class VersionController : MonoBehaviour
    {
        public VisualTreeAsset accordionEntryTemplate; // Assign the UXML template in the Inspector
        private List<VersionData> versions;
        private VisualElement root;

        private void Start()
        {
            ShowAvailableVersions();
        }

        public async void ShowAvailableVersions()
        {
            string versionString = await File.ReadAllTextAsync(UpdateManager.Instance.GetReleaseDataPath());
            versions = JsonConvert.DeserializeObject<List<VersionData>>(versionString); 
            root = GetComponent<UIDocument>().rootVisualElement;
            PopulateAccordion();
        }

        private void PopulateAccordion()
        {
            foreach (var version in versions)
            {
                var accordionEntry = accordionEntryTemplate.CloneTree();
                accordionEntry.Q<Label>("VersionName").text = version.versionName;
                accordionEntry.Q<Label>("ReleaseTitle").text = version.releaseTitle;
                accordionEntry.Q<Label>("ReleaseNotes").text = version.releaseNotes;

                var content = accordionEntry.Q<VisualElement>("Content");
                var headerButton = accordionEntry.Q<Button>("Header");
                var downloadButton = accordionEntry.Q<Button>("DownloadButton");

                // Toggle accordion content visibility
                headerButton.clicked += () =>
                {
                    version.isExpanded = !version.isExpanded;
                    content.style.display = version.isExpanded ? DisplayStyle.Flex : DisplayStyle.None;
                };

                // Download button functionality
                downloadButton.clicked += () =>
                {
                    Debug.Log($"Downloading: {version.exeName} with File ID: {version.fileId}");
                    // Implement actual download logic here
                };

                // Add to the root
                root.Add(accordionEntry);
            }
        }
    }
}