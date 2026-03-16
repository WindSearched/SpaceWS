using System.Collections.Generic;
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
    public float listheitght = 0;
    private void Start()
    {
        ct.cmdPg = this;

        var e = ct.action.Add("enter",InputActionType.Button);
        ct.action.AddBiding(e,SAction.keyTable["enter"]);

        ct.pages.Register("command", new(_ =>
        {
            ct.UnlockMouse();
            e.performed += ToCommand;
            parent.SetActive(true);
        }, _ =>
        {
            ct.LockMouse();
            parent.SetActive(false);
            e.performed -= ToCommand;

            text.text = "";
        }));

        //add an action to active the command page
        var b = ct.action.Add("commandButton",InputActionType.Button);
        ct.action.AddBiding(b,SAction.keyTable["f2"]);
        b.performed += c => ct.pages.Swap(ct.pages.IsPage("command") ? "main" : "command");

        button.onClick.AddListener(ToCommand);

        orMsg = Resources.Load<GameObject>("ui/logmessage");

        ct.command.Add("debug", (l) =>
        {
            var s = l.Load();
            Debug.Log(s);
            Message(s);
        });
        ct.command.Add("load", l => // /load meteorite 0 0 0 0 0 0
        {
            var cu = ct.setting.chunkUnit;

            var type = l.Load();
            var pos = l.LoadV3();
            var rot = l.LoadV3();
            var cp = pos.intDivision(cu);
            pos %= cu;
            ct.bodies.LoadStruct(pos, cp, rot, -1, type);
        });
        ct.command.Add("exit", l =>
        {
            ct.ExitGame();
        });

        ct.command.AddValueMethod("rand", () =>//@rand
        {
            return SMath.Random(int.MaxValue, int.MinValue);
        });

        Message("test");
    }

    void ToCommand(InputAction.CallbackContext context) => ToCommand();
    void ToCommand()
    {
        var c = text.text;
        ct.command.Load(c);
        text.text = "";
    }

    public void Message(string msg)
    {
        GameObject o = Instantiate(orMsg, listparent);
        var tx = o.GetComponent<TextMeshProUGUI>();
        var rt =  o.GetComponent<RectTransform>();
        tx.text = msg;
        tx.ForceMeshUpdate();
        float h = tx.preferredHeight;
        rt.sizeDelta = new(rt.sizeDelta.x, h);

        rt.anchoredPosition = new(0, -listheitght);
        listheitght += h;
    }
}
