using In.App.Update.DataModel;

namespace In.App.Update
{
    public abstract class BaseAppUpdater
    {
        public abstract void StartNewBuild(VersionData versionData);
    }
}