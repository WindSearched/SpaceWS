using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SAction
{
    /// <summary>
    /// The correspondence table from key general name to name can load
    /// </summary>
    public static Dictionary<string, string> keyTable =
    new Dictionary<string, string>()
        {
            // 字母
            { "a", "<Keyboard>/a" },
            { "b", "<Keyboard>/b" },
            { "c", "<Keyboard>/c" },
            { "d", "<Keyboard>/d" },
            { "e", "<Keyboard>/e" },
            { "f", "<Keyboard>/f" },
            { "g", "<Keyboard>/g" },
            { "h", "<Keyboard>/h" },
            { "i", "<Keyboard>/i" },
            { "j", "<Keyboard>/j" },
            { "k", "<Keyboard>/k" },
            { "l", "<Keyboard>/l" },
            { "m", "<Keyboard>/m" },
            { "n", "<Keyboard>/n" },
            { "o", "<Keyboard>/o" },
            { "p", "<Keyboard>/p" },
            { "q", "<Keyboard>/q" },
            { "r", "<Keyboard>/r" },
            { "s", "<Keyboard>/s" },
            { "t", "<Keyboard>/t" },
            { "u", "<Keyboard>/u" },
            { "v", "<Keyboard>/v" },
            { "w", "<Keyboard>/w" },
            { "x", "<Keyboard>/x" },
            { "y", "<Keyboard>/y" },
            { "z", "<Keyboard>/z" },

            // 数字（主键盘）
            { "0", "<Keyboard>/0" },
            { "1", "<Keyboard>/1" },
            { "2", "<Keyboard>/2" },
            { "3", "<Keyboard>/3" },
            { "4", "<Keyboard>/4" },
            { "5", "<Keyboard>/5" },
            { "6", "<Keyboard>/6" },
            { "7", "<Keyboard>/7" },
            { "8", "<Keyboard>/8" },
            { "9", "<Keyboard>/9" },

            // 功能键
            { "space", "<Keyboard>/space" },
            { "enter", "<Keyboard>/enter" },
            { "esc", "<Keyboard>/escape" },
            { "tab", "<Keyboard>/tab" },
            { "backspace", "<Keyboard>/backspace" },

            // 修饰键
            { "shift", "<Keyboard>/shift" },
            { "ctrl", "<Keyboard>/ctrl" },
            { "alt", "<Keyboard>/alt" },

            // 方向键
            { "up", "<Keyboard>/upArrow" },
            { "down", "<Keyboard>/downArrow" },
            { "left", "<Keyboard>/leftArrow" },
            { "right", "<Keyboard>/rightArrow" },

            // 功能键 F1~F12
            { "f1", "<Keyboard>/f1" },
            { "f2", "<Keyboard>/f2" },
            { "f3", "<Keyboard>/f3" },
            { "f4", "<Keyboard>/f4" },
            { "f5", "<Keyboard>/f5" },
            { "f6", "<Keyboard>/f6" },
            { "f7", "<Keyboard>/f7" },
            { "f8", "<Keyboard>/f8" },
            { "f9", "<Keyboard>/f9" },
            { "f10", "<Keyboard>/f10" },
            { "f11", "<Keyboard>/f11" },
            { "f12", "<Keyboard>/f12" },

            {"v2","2DVector"},
            {"mouse","<Mouse>/position"},
            {"1DAxis","Positive / Negative"},
            {"ButtonWithOneModifier","Modifier / Button"},
            {"ButtonWithOneModifiers","Modifier1 / Modifier2 / Button"},
            {"mouseDelta","<Mouse>/delta"},
        };
    public Dictionary<string,InputAction> acts = new();

    /// <summary>
    /// Add an action
    /// </summary>
    /// <param name="key"></param>
    /// <param name="biding"></param>
    /// <param name="type"></param>
    public InputAction Add(string key,InputActionType type,string biding = null)
    {
        if (biding == "")
            return null;

        InputAction ia = new(
            type: type,
            binding: biding
        );

        ia.Enable();
        acts.Add(key, ia);
        ct.log.Write("Actions",$"Load a action, keyname:{key}, biding:{biding}, {type.ToString()}");

        return ia;
    }

    public void AddBiding(InputAction ia, string biding,string interactions = null,
        string processors = null, string groups = null)
    {
        ia.AddBinding(biding, interactions, processors, groups);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="ia"></param>
    /// <param name="composite">composite type</param>
    /// <param name="paths"></param>
    /// <param name="interactions"></param>
    /// <param name="processors"></param>
    public void AddComposite(InputAction ia, string composite,List<(string path,string part)> paths,
        string interactions = null, string processors = null)
    {
        var v =ia.AddCompositeBinding(composite, interactions, processors);
        foreach (var path in paths)
        {
            v.With(path.part,path.path);
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="ia"></param>
    /// <param name="bindings">order : up, down, left, right</param>
    public void AddVector2(InputAction ia, string[] bindings = null)
    {
        bindings ??= new[]
        {
            keyTable["w"],
            keyTable["s"],
            keyTable["a"],
            keyTable["d"],
        };
        List<(string path, string part)> paths = new List<(string path, string part)>()
        {
            new(){path=bindings[0],part="Up"},
            new(){path=bindings[1],part="Down"},
            new(){path=bindings[2],part="Left"},
            new(){path=bindings[3],part="Right"},
        };
        AddComposite(ia,keyTable["v2"], paths);
    }

    public int GetBindingIndex(InputAction action, string path)
    {
        for (int i = 0; i < action.bindings.Count; i++)
        {
            if (action.bindings[i].effectivePath == path)
                return i;
        }
        return -1;
    }

    public InputAction GetAction(string key)
    {
        if (acts.TryGetValue(key, out var action)) return action;
        Debug.LogError($"[GetAction]Key {key} not found");
        return null;
    }
    public void ChangeAction(string key, string overrided, string biding)
    {
        var act = GetAction(key);
        int idx = GetBindingIndex(act, overrided);

        act.ApplyBindingOverride(idx, "biding");
    }
}