using System;
using UnityEngine;
using UnityEngine.EventSystems;

sealed class PointerUpDownListener : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
{
    public bool isDown;

    public Action PointerUpAction { private get; set; }
    public Action PointerDownAction { private get; set; }

    public void OnPointerUp(PointerEventData data)
    {
        isDown = false;
        PointerUpAction?.Invoke();
    }

    public void OnPointerDown(PointerEventData data)
    {
        isDown = true;
        PointerDownAction?.Invoke();
    }
}
