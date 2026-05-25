using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CommandPage : MonoBehaviour
{
    public TextMeshProUGUI text;
    public Button button;
    public GameObject parent;
    public Transform listparent;
    public GameObject orMsg;
    public float listheitght;

    private void Start()
    {
        ct.cmdPg = this;

        var e = ct.action.Add("enter", InputActionType.Button);
        ct.action.AddBiding(e, SAction.keyTable["enter"]);

        ct.pages.Register("command", new Page(_ =>
        {
            ct.UnlockMouse();
            ct.cameraMove.AddPin("cmd");
            ct.playerMove.AddPin("cmd");
            e.performed += ToCommand;
            parent.SetActive(true);
        }, _ =>
        {
            ct.LockMouse();
            ct.cameraMove.RemovePin("cmd");
            ct.playerMove.RemovePin("cmd");
            parent.SetActive(false);
            e.performed -= ToCommand;

            text.text = "";
        }));

        //add an action to active the command page
        var b = ct.action.Add("commandButton", InputActionType.Button);
        ct.action.AddBiding(b, SAction.keyTable["f2"]);
        b.performed += c => ct.pages.Swap(ct.pages.IsPage("command") ? "main" : "command");

        button.onClick.AddListener(ToCommand);

        orMsg = Resources.Load<GameObject>("ui/logmessage");

        CommandBranch command(string name) => new CommandBranch(name);


        ct.CommandBranch.AddBranch(
            command("debug")
                .AddArgument(new CommandBranch.Argument("text").SetSuggestion(() => new List<string>{"Wind_searched"}))
                .Execute((arg, load) =>
                {
                    var s =load.LoadString(arg.Get("text"));
                    Debug.Log(s);
                    Message(s);
                    return true;
                }));
        ct.CommandBranch.AddBranch(
            command("load")
                .AddArguments(
                    new("smType"), new("position"), new ("rotation"))// 0;0;0
                .Execute((arg, load) =>
                {
                    try
                    {
                        var t = arg.Get("smType");
                        if (!SMType.TryParse(t,  out var smt))
                        {
                            smt = ct.structsInfo.GetFirstKey(t);
                        }

                        var pos = load.LoadV3("position");
                        var rot = load.LoadV3("rotation");
                        ct.bodies.LoadStruct(pos, rot, -1, smt);

                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                })
        );

        ct.CommandBranch.AddBranch(command("exit")
            .Execute((_, _) =>
            {
                ct.ExitGame();
                return true;
            }));
        ct.CommandBranch.AddBranch(command("modify"))
            .AddArguments(
                new CommandBranch.Argument("indicate"),
                new("param"),
                new("value")
            )
            .Execute((arg, load) =>
            {
                var id = load.LoadString(arg.Get("indicate"));
                int idx = int.Parse(id);
                var state =ct.bodies.datas[idx].self;
                var vt = arg.Get("param");
                var val = load.LoadInt(arg.Get("value"));
                FieldInfo fi = typeof(BodyState).GetField(vt);
                if (fi == null)
                {
                    Message($"The value type '{vt}' is not exist in bodystate");
                    return false;
                }
                else
                {
                    fi.SetValue(state, val);
                    return true;
                }
            });
        ct.CommandBranch.AddBranch(command("print"))
            .AddArguments(new("indicate"), new("param"))
            .Execute((arg, load) =>
            {
                object o = load.Load(arg.Get("indicate"));
                var p = arg.Get("param");

                string s = "";

                if (o is GameObject g)//get struct state
                {
                    int id = int.Parse(g.transform.parent.parent.name);
                    var ss = ct.bodies.datas[id].structs[int.Parse(g.name)];
                    string vt = p;
                    s= STool.GetNestedToString(ss, vt);
                }
                else if(o == null)
                {
                    return false;
                }
                else
                {
                    s = o.ToString();
                }

                if (s == "") return false;
                ct.log.Write("cmd.print", s);
                Message(s);
                return true;
            });

        ct.methValues.Reg("rand", () => //@rand
            SMath.Random(int.MaxValue, int.MinValue));
        ct.methValues.Reg("indicated", () => //@indicated
            ct.mouseCasted ? ct.mouseCasted.transform.parent.parent.name : "0");
        ct.methValues.Reg("indicatedObj", () => ct.mouseCasted);
    }

    private void ToCommand(InputAction.CallbackContext context)
    {
        ToCommand();
    }

    private void ToCommand()
    {
        var c = text.text.TrimStart('\u200B');
        ct.CommandBranch.Command(c);
        text.text = "";
    }

    public void Message(string msg)
    {
        var o = Instantiate(orMsg, listparent);
        var tx = o.GetComponent<TextMeshProUGUI>();
        var rt = o.GetComponent<RectTransform>();
        tx.text = msg;
        tx.ForceMeshUpdate();
        var h = tx.preferredHeight;
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, h);

        rt.anchoredPosition = new Vector2(0, -listheitght);
        listheitght += h;
    }
}