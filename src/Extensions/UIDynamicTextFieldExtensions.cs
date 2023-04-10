using UnityEngine.UI;

public static class UIDynamicTextFieldExtensions
{
    public static void DisableScrollOnText(this UIDynamicTextField target)
    {
        var scrollRect = target.UItext.transform.parent.transform.parent.transform.parent.GetComponent<ScrollRect>();
        if(scrollRect)
        {
            scrollRect.horizontal = false;
            scrollRect.vertical = false;
        }
    }
}
