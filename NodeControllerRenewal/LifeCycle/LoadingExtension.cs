namespace NodeController.LifeCycle
{
    using ICities;
    using NodeController30;

    public class LoadingExtention : LoadingExtensionBase
    {
        public override void OnLevelLoaded(LoadMode mode)
        {
            Mod.Logger.Debug("LoadingExtention.OnLevelLoaded");

            LifeCycle.OnLevelLoaded(mode);
        }

        public override void OnLevelUnloading()
        {
            Mod.Logger.Debug("LoadingExtention.OnLevelUnloading");
            LifeCycle.OnLevelUnloading();
        }
    }
}
