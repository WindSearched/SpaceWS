using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SButton : Button
{
    [Header("Pointer Events")]
    public UnityEvent OnButtonDown;
    public UnityEvent OnButtonUp;
    public UnityEvent OnButtonEnter;
    public UnityEvent OnButtonExit;

    public event sButtonMeth onButtonDown;
    public event sButtonMeth onButtonUp;
    public event sButtonMeth  onButtonEnter;
    public event sButtonMeth  onButtonExit;


    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        OnButtonDown.Invoke();
        onButtonDown?.Invoke(gameObject);
    }
    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        OnButtonUp.Invoke();
        onButtonUp?.Invoke(gameObject);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        OnButtonEnter?.Invoke();
        onButtonEnter?.Invoke(gameObject);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        OnButtonExit?.Invoke();
        onButtonExit?.Invoke(gameObject);
    }

    public void ClearDown()
    {
        onButtonDown = null;
        OnButtonDown = null;
    }
    public void ClearUp()
    {
        onButtonUp = null;
        OnButtonUp = null;
    }
    public void ClearEnter()
    {
        onButtonEnter = null;
        OnButtonEnter = null;
    }
    public void ClearExit()
    {
        onButtonExit = null;
        OnButtonExit = null;
    }
}
public delegate void sButtonMeth(GameObject g);