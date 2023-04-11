using UnityEngine;

static class UIDynamicButtonExtensions
{
    public static void SetInvisibleFocusedColor(this UIDynamicButton uiDynamicButton)
    {
        var colors = uiDynamicButton.button.colors;
        colors.highlightedColor = Color.white;
        colors.pressedColor = Color.white;
        uiDynamicButton.button.colors = colors;
    }

    public static void SetNormalFocusedColor(this UIDynamicButton uiDynamicButton)
    {
        var colors = uiDynamicButton.button.colors;
        colors.highlightedColor = Colors.defaultBtnHighlightedColor;
        colors.pressedColor = Colors.defaultBtnPressedColor;
        uiDynamicButton.button.colors = colors;
    }
}
