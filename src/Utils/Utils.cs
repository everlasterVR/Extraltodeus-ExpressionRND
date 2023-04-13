using UnityEngine;

static class Utils
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

    public static float RoundToDecimals(float value, float roundFactor = 1000f)
    {
        return Mathf.Round(value * roundFactor) / roundFactor;
    }
}
