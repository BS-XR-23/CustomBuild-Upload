using Newtonsoft.Json;

namespace In.App.Update.DataModel
{
    public class VersionData
    {
        public string versionName;
        public int versionCode;
        public string fileId;
        public string exeName;
        public string releaseTitle;
        public string releaseNotes;
        [JsonIgnore]
        public bool isExpanded;
    }
}