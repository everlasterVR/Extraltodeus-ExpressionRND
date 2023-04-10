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
}
