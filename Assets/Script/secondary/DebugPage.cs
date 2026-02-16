using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DebugPage : MonoBehaviour
{
    private static int linesNumber => ct.setting.debugLineNumber;
    private static bool showId => ct.setting.debugShowIndex;
    private static Vector2 screenSize => ct.screenSize;
    private static float textHeight;
    private bool active = false;
    public Transform parent;
    public GameObject template;
    private SDebug debugInfo => ct.debugInfo;
    private List<TextMeshProUGUI> left_texts = new();
    private List<TextMeshProUGUI> right_texts = new();



    private void Start()
    {
        float sizex = screenSize.x;

        textHeight = screenSize.y / linesNumber;
        left_texts.Capacity = right_texts.Capacity = linesNumber;
        for (int i = 0; i < linesNumber; i++)//initialize
        {

            var left = Instantiate(template, parent);
            var lrt = left.GetComponent<RectTransform>();
            var ltx = left.GetComponent<TextMeshProUGUI>();
            var right = Instantiate(template, parent);
            var rrt = right.GetComponent<RectTransform>();
            var rtx = right.GetComponent<TextMeshProUGUI>();

            left.name = i + "left";
            right.name = i + "right";

            rrt.sizeDelta = lrt.sizeDelta = new(sizex, textHeight);
            rrt.anchoredPosition = lrt.anchoredPosition = new(0, -i * textHeight);
            rtx.fontSize = ltx.fontSize = textHeight;
            rtx.alignment = TextAlignmentOptions.MidlineRight;
            right_texts.Add(rtx);
            left_texts.Add(ltx);
        }

        ct.pages.Register("debugPage", new(parent, () =>
        {
            ct.UnlockMouse();
            active = true;
            parent.gameObject.SetActive(active);
            RegToTick();
        },
        () =>
        {
            ct.LockMouse();
            active = false;
            parent.gameObject.SetActive(active);
        }));

        var act = ct.action.Add("debugPage", InputActionType.Button, SAction.keyTable["f3"]);
        act.performed += c =>
        {
            active = !active;
            if(active)
                ct.pages.Swap("debugPage");
            else
                ct.pages.Swap("main");
        };
    }

    public void RegToTick()
    {
        Tick.Reg(rg =>
        {
            if (active)
                Tick.Reg(rg);
            else
                rg.repeattime = 0;

            for (int i = 0; i < linesNumber; i++)
            {
                var lrt = left_texts[i];
                var lf = debugInfo.left_funcs[i];
                if (lf != null)
                {
                    string t = "";
                    if (showId) t += i + "\t";
                    t += lf.name;
                    t += " : ";
                    t += lf.func?.Invoke();
                    lrt.text = t;
                }

                var rrt = right_texts[i];
                var rf = debugInfo.right_funcs[i];
                if (rf != null)
                {
                    string t = "";
                    t += rf.name;
                    t += " : ";
                    t += rf.func?.Invoke();
                    rrt.text = t;
                }
            }
        },1, -100, 1);
    }

    private void OnRectTransformDimensionsChange()
    {
        Debug.Log("OnRectTransformDimensionsChange");
        float sx = screenSize.x;
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            var child = parent.transform.GetChild(i);
            child.GetComponent<RectTransform>().sizeDelta = new(sx,textHeight);
        }
    }
}
public delegate string debugFunc();

public class SDebug
{
    public List<SFunc> left_funcs = new();
    public List<SFunc> right_funcs = new();
    private int leftInit, rightInit;
    private int linesNumber;
    public class SFunc
    {
        public debugFunc func;
        public string name = "";
    }

    public SDebug(int linesNumber)
    {
        left_funcs.Capacity = right_funcs.Capacity = linesNumber;
        for (int i = 0; i < linesNumber; i++) //initialize
        {
            left_funcs.Add(null);
            right_funcs.Add(null);
        }
        this.linesNumber = linesNumber;
    }

    public void LeftRegister(debugFunc func, string name, int index)
    {
        var f = new SFunc();
        f.name = name;
        f.func = func;

        if(index >= leftInit)
            leftInit = index + 1;
        left_funcs[index] = f;
    }
    public void RightRegister(debugFunc func, string name, int index)
    {
        var f = new SFunc();
        f.name = name;
        f.func = func;

        if(index >= rightInit)
            rightInit = index + 1;
        right_funcs[index] = f;
    }

    public void LeftAdd(debugFunc func, string name)
    {
        int id = LeftFindVoidLine(leftInit);
        if(id == -1)
            return;
        LeftRegister(func, name, id);
    }

    public void RightAdd(debugFunc func, string name)
    {
        int id = RightFindVoidLine(rightInit);
        if(id == -1)
            return;
        RightRegister(func, name, id);
    }



    /// <returns>if is -1 the register is full</returns>
    public int LeftFindVoidLine(int initIndex)
    {
        for (int i = initIndex; i < linesNumber; i++)// start check from initindex
        {
            if (left_funcs[i] == null)
                return i;
        }
        for (int i = 0; i < initIndex; i++)//if between initindex and linesnumber is full check to start of list
        {
            if (left_funcs[i] == null)
                return i;
        }
        return -1;//if all line is full return -1
    }
    /// <returns>if is -1 the register is full</returns>
    public int RightFindVoidLine(int initIndex)
    {
        for (int i = initIndex; i < linesNumber; i++)// start check from initindex
        {
            if (right_funcs[i] == null)
                return i;
        }
        for (int i = 0; i < initIndex; i++)//if between initindex and linesnumber is full check to start of list
        {
            if (right_funcs[i] == null)
                return i;
        }
        return -1;//if all line is full return -1
    }
}