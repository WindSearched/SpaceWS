using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SButton : Button
{
    [Header("Pointer Events")]
    public UnityEvent OnButtonDown;
    public UnityEvent OnButtonUp;
    public override void OnSubmit(BaseEventData eventData)
    {
        // ���� Enter
        // ������ base.OnSubmit(eventData);
    }
    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        Debug.Log("down"); 
        OnButtonDown.Invoke();
    }
    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        Debug.Log("up");
        OnButtonUp.Invoke();
    }
}
