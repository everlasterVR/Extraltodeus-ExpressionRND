using System.Linq;

static class Utils
{
    // Function taken from VAMDeluxe's code :)

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
