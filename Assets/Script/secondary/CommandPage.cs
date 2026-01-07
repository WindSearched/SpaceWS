using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CommandPage : MonoBehaviour
{
    public TextMeshProUGUI text;
    public Button button;
    public GameObject parent;
    private void Start()
    {
        var e = ct.action.Add("enter",InputActionType.Button);
        ct.action.AddBiding(e,SAction.keyTable["enter"]);

        ct.pages.Register("command", new(parent.transform, () =>
        {
            ct.UnlockMouse();
            e.performed += ToCommand;
            parent.SetActive(true);
        }, () =>
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
    }

    void ToCommand(InputAction.CallbackContext context) => ToCommand();
    void ToCommand()
    {
        var c = text.text;
        ct.command.Load(c);
        text.text = "";
    }
}
