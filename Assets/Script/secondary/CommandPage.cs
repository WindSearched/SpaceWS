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

        ct.command.Add("debug", l =>
        {
            var s = l.Load();
            Debug.Log(s);
            Message(s);
        });
        ct.command.Add("load", l => // /load Smod/meteorite 0 0 0 0 0 0
        {
            var cu = ct.setting.chunkUnit;

            var t = l.Load();
            if (!SMType.TryParse(t,  out var smt))
            {
                smt = ct.structsInfo.GetFirstKey(l.Load());
            }

            var pos = l.LoadV3();
            var rot = l.LoadV3();
            var cp = pos.intDivision(cu);
            pos %= cu;
            ct.bodies.LoadStruct(pos, cp, rot, -1, smt);
        });
        ct.command.Add("exit", l => { ct.ExitGame(); });
        ct.command.Add("modify", l =>// modify @indicated temperature 100
        {
            var id = l.Load();
            Debug.Log(id);
            int idx = int.Parse(id);
            var state =ct.bodies.datas[idx].self;
            var vt = l.Load();
            FieldInfo fi = typeof(BodyState).GetField(vt);
            if (fi == null)
            {
                Message($"The value type '{vt}' is not exist in bodystate");
            }
            else
            {
                fi.SetValue(state, l.Load());
            }
        });
        ct.command.Add("addheat", l => // addheat @indicated 200
        {
            int id = int.Parse(l.Load());
            ct.bodies.AddHeat(id, l.LoadFloat());
        });
        ct.command.Add("print", l =>// print @indicatedO mixture
        {
            object o = l.LoadObj();

            string s = "";

            if (o is GameObject g)//get struct state
            {
                int id = int.Parse(g.transform.parent.parent.name);
                var ss = ct.bodies.datas[id].structs[int.Parse(g.name)];
                string vt = l.Load();
                s= STool.GetNestedToString(ss, vt);
            }
            else if(o == null)
            {
                return;
            }
            else
            {
                s = o.ToString();
            }

            if (s == "") return;
            ct.log.Write("cmd.print", s);
            Message(s);
        });

        ct.command.AddValueMethod("rand", () => //@rand
            SMath.Random(int.MaxValue, int.MinValue));
        ct.command.AddValueMethod("indicated", () => //@indicated
            ct.mouseCasted ? ct.mouseCasted.transform.parent.parent.name : "0");
        ct.command.AddValueMethod("indicatedO", () => ct.mouseCasted);
    }

    private void ToCommand(InputAction.CallbackContext context)
    {
        ToCommand();
    }

    private void ToCommand()
    {
        var c = text.text;
        ct.command.Load(c);
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