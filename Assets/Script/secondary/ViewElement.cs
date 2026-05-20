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

    private bool started = false;

    void Start()
    {
        if(started) return;
        started = true;

        text = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        image = GetComponent<Image>();
        rect = GetComponent<RectTransform>();
        button = GetComponent<SButton>();

        Refresh();
    }

    public void Refresh()
    {
        if(!started)
            Start();

        var b = _base;

        if (b.text != null)
        {
            text.text = b.text;
            text.color = b.textColor;
        }

        image.sprite = b.sprite;
        image.color = b.backColor;

        rect.pivot = b.pivot;
        rect.sizeDelta = b.size;
        rect.anchoredPosition = b.position + b.offset;

        button.ClearDown();
        button.onButtonDown += g =>  b.downInput?.Invoke(this, g);
        button.ClearUp();
        button.onButtonUp += g =>  b.upInput?.Invoke(this, g);
        button.ClearEnter();
        button.onButtonEnter += g =>  b.enterInput?.Invoke(this, g);
        button.ClearExit();
        button.onButtonExit += g =>  b.exitInput?.Invoke(this, g);

        Tick.Reg(t =>
        {
            if (!updatestop)
            {
                b.update?.Invoke(this);
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
