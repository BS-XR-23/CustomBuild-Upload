using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using In.App.Update.DataModel;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

namespace In.App.Update
{
    public class VersionController : MonoBehaviour
    {
        public VisualTreeAsset accordionEntryTemplate; // Assign the UXML template in the Inspector
        private List<VersionData> versions;
        private VisualElement AccordionRoot;
        private VisualElement AccordionContainer;
        private VisualElement DialogBox;
        
        private Button UpdateButton;
        private Button CloseButton;
        private Button YesButton;
        private Button NoButton;
        private ProgressBar progressBar;
        private Button cancelButton;
        private VisualElement progressDialog;
        private Label CurrentVersion;
        private void Start()
        {
            Initialize();
            
        }

        private void Initialize()
        {
            VisualElement rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
            AccordionRoot = rootVisualElement.Q<VisualElement>("AccordionRoot");
            AccordionContainer = rootVisualElement.Q<VisualElement>("AccordionContainer");
            AccordionContainer.style.display = DisplayStyle.None;
            
            CloseButton = rootVisualElement.Q<Button>("CloseButton");
            DialogBox = rootVisualElement.Q<VisualElement>("DialogBox");
            UpdateButton = rootVisualElement.Q<Button>("UpdateButton");
            YesButton = rootVisualElement.Q<Button>("YesButton");
            NoButton = rootVisualElement.Q<Button>("NoButton");
            
            UpdateButton.clicked += () => AccordionContainer.style.display = DisplayStyle.Flex;
            CloseButton.clicked += () => AccordionContainer.style.display = DisplayStyle.None;
            YesButton.clicked += () =>
            {
                DialogBox.style.display = DisplayStyle.None;
                Restart();
            };
            NoButton.clicked += () => DialogBox.style.display = DisplayStyle.None;
            
            progressDialog = rootVisualElement.Q<VisualElement>("download-dialog");
            progressBar = progressDialog.Q<ProgressBar>("progress-bar");
            cancelButton = progressDialog.Q<Button>("cancel-button");
            progressBar.value = 0;
            // Hide the dialog initially
            progressDialog.style.display = DisplayStyle.None;

            // Set up the Cancel button
            cancelButton.clicked += () =>
            {
                CancelDownload();
                progressDialog.style.display = DisplayStyle.None; // Hide dialog
            };
            CurrentVersion = rootVisualElement.Q<Label>("CurrentVersion");
            CurrentVersion.text = $"Current Version: v{Application.version}";
            UpdateButton.style.display=DisplayStyle.None;
            isUpdateAvailable();
        }
        private async void isUpdateAvailable()
        {
           bool isUpdate= await UpdateManager.Instance.IsUpdateAvailable(Application.version);
           if (isUpdate)
           {
                UpdateButton.style.display=DisplayStyle.Flex;
                UpdateButton.clicked += ShowAvailableVersions;
           }
        }
        public async void ShowAvailableVersions()
        {
            versions= await UpdateManager.Instance.GetAvailableVersions();
            PopulateAccordion();
        }
        private void CancelDownload()
        {
            UpdateManager.Instance.CancelDownload();
            // Implement download cancellation logic here
        }
        private async void Restart()
        {
            await UpdateManager.Instance.Restart();
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
                    AccordionContainer.style.display = DisplayStyle.None;
                    DownloadVersion(version);
                    // Implement actual download logic here
                };
                // Add to the root
                AccordionRoot.Add(accordionEntry);
            }
        }
        private async void DownloadVersion(VersionData version)
        {
            progressDialog.style.display = DisplayStyle.Flex;
            await UpdateManager.Instance.DownloadVersion(version,onProgress: async (progress) =>
            {
                Debug.Log($"progress:{progress}");
                progressBar.value = progress;
                //progressDialog.style.display = DisplayStyle.None;
               // progressBar.value = progress;
            });
            progressDialog.style.display = DisplayStyle.None;
            DialogBox.style.display = DisplayStyle.Flex;
        }
    }
   
}