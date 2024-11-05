namespace everlaster
{
    static partial class Utils
    {
        public static bool GlobalAnimationFrozen()
        {
            bool mainToggleFrozen =
                SuperController.singleton.freezeAnimationToggle &&
                SuperController.singleton.freezeAnimationToggle.isOn;
            bool altToggleFrozen =
                SuperController.singleton.freezeAnimationToggleAlt &&
                SuperController.singleton.freezeAnimationToggleAlt.isOn;
            return mainToggleFrozen || altToggleFrozen;
        }
    }
}
