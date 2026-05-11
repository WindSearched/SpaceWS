using UnityEngine.UI;
using TMPro;
using UnityEngine;

public class ViewElement : MonoBehaviour
{
    public ViewElementBase _base;

    public TextMeshProUGUI text;
    public Image image;
    public RectTransform rect;
    public SButton button;

    public bool stop => gameObject.activeSelf;
    public bool updatestop = false;

    void Start()
    {
        text = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        image = GetComponent<Image>();
        rect = GetComponent<RectTransform>();

        Refresh();
    }

    public void Refresh()
    {
        var b = _base;

        text.text = b.text;
        image.sprite = b.sprite;

        rect.pivot = b.pivot;
        rect.sizeDelta = b.size;
        rect.anchoredPosition = b.position;

        button.ClearDown();
        button.onButtonDown += g =>  b.downInput.Invoke(this, g);
        button.ClearUp();
        button.onButtonUp += g =>  b.upInput.Invoke(this, g);
        button.ClearEnter();
        button.onButtonEnter += g =>  b.enterInput.Invoke(this, g);
        button.ClearExit();
        button.onButtonExit += g =>  b.exitInput.Invoke(this, g);

        Tick.Reg(t =>
        {
            if (!updatestop)
            {
                b.update.Invoke(this);
            }
            else
            {
                updatestop = false;
                t.loop = 0;
            }
        },1, -1);
    }

    public void SetBase(ViewElementBase element,bool refresh)
    {
        _base = element;
        if (refresh)
            Refresh();
    }
}
