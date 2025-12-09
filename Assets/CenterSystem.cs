using UnityEngine;

public class CenterSystem : MonoBehaviour
{
    private void Start()
    {
        Tick.Reg(new()//update position every tick
        {
            offset = 1,
            onTick = (TickReg reg) =>
            {
                ct.UpdatePerTick();

                var p = ct.act.main.move.ReadValue<Vector2>();// player position
                ct.playerCanMove = p != Vector2.zero;
                ct.wasdDirection = p;

                ct.fps = 1f / Time.unscaledDeltaTime;

                Tick.Reg(reg);
            }
        });
    }
    private void Update()
    {
        var v = ct.act.main.mouse.ReadValue<Vector2>(); //
        ct.mouseDirection = v - ct.mousePosition;       //  Get mouse data
        ct.mousePosition = v;                           //  


    }
    private void Awake()
    {
        ct.act = new Actions();
    }
    private void OnEnable()
    {
        ct.act.Enable();
    }
    private void OnDisable()
    {
        ct.act.Disable();
    }

}
