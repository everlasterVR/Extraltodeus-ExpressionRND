using System;
using UnityEngine;
using UnityEngine.UI;

static class UIDynamicExtensions
{
    public static void SetVisible(this UIDynamic uiDynamic, bool visible)
    {
        if(!uiDynamic)
        {
            return;
        }

        var layoutElement = uiDynamic.GetComponent<LayoutElement>();
        if(layoutElement)
        {
            layoutElement.transform.localScale = visible ? new Vector3(1, 1, 1) : new Vector3(0, 0, 0);
            layoutElement.ignoreLayout = !visible;
        }
    }

    public static void SetActiveStyle(this UIDynamic element, bool isActive, bool setInteractable = false, bool highlightIneffective = false)
    {
        if(!element)
        {
            return;
        }

        var color = isActive ? Color.black : Colors.inactive;
        if(element is UIDynamicSlider)
        {
            var uiDynamicSlider = (UIDynamicSlider) element;
            uiDynamicSlider.slider.interactable = !setInteractable || isActive;
            uiDynamicSlider.quickButtonsEnabled = !setInteractable || isActive;
            uiDynamicSlider.defaultButtonEnabled = !setInteractable || isActive;
            uiDynamicSlider.labelText.color = color;
        }
        else if(element is UIDynamicToggle)
        {
            var uiDynamicToggle = (UIDynamicToggle) element;
            uiDynamicToggle.toggle.interactable = !setInteractable || isActive;
            if(highlightIneffective && uiDynamicToggle.toggle.isOn && uiDynamicToggle.toggle.interactable)
            {
                color = isActive ? Color.black : Color.red;
            }

            uiDynamicToggle.labelText.color = color;
        }
        else if(element is UIDynamicButton)
        {
            var uiDynamicButton = (UIDynamicButton) element;
            uiDynamicButton.button.interactable = !setInteractable || isActive;
            var colors = uiDynamicButton.button.colors;
            colors.disabledColor = colors.normalColor;
            uiDynamicButton.button.colors = colors;
            uiDynamicButton.textColor = color;
        }
        else
        {
            throw new ArgumentException($"UIDynamic {element.name} was null, or not an expected type");
        }
    }
}
