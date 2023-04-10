using UnityEngine;
using UnityEngine.Events;

sealed class UnityEventsListener : MonoBehaviour
{
    public readonly UnityEvent onEnable = new UnityEvent();
    public readonly UnityEvent onDisable = new UnityEvent();

    void OnEnable()
    {
        onEnable.Invoke();
    }

    void OnDisable()
    {
        onDisable.Invoke();
    }

    void OnDestroy()
    {
        onEnable.RemoveAllListeners();
        onDisable.RemoveAllListeners();
    }
}
