using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Indicator : MonoBehaviour
{
    private Vector2 mp => ct.mousePosition;
    private Dictionary<string, Sprite> indicators => ct.indicatorSprites;
    public Image image;
    public RectTransform rt;
    private void FixedUpdate()
    {
        rt.position = mp;
    }

    private void Start()
    {
        ct.indicator = this;
    }

    /// <summary>
    /// swap indicator
    /// </summary>
    public bool Swap(string name)
    {
        if(!indicators.ContainsKey(name)) return false;
        image.sprite = indicators[name];
        return true;
    }
    public void Active(bool active) => gameObject.SetActive(active);
}