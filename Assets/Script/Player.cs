using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public Entity entity;
    /// <summary>
    /// current Move Mode
    /// </summary>
    public MoveMode cMvMd;

    private void Start()
    {
        entity = GetComponent<Entity>();
        
        moveModes.Add("LevelMove",new() { 
            OnMove = (Vector2 dir, Rigidbody rig) =>
            {
                rig.AddForce(dir * 10);
            },
            OnStop = (Rigidbody rig) =>
            {
                rig.linearVelocity = Vector3.zero;
            },
            Name = "LevelMove"
        });
        moveModes.Add("CameraFrontDirectionMove", new() {
            OnMove = (Vector2 dir, Rigidbody rig) =>
            {

                Debug.Log(ct.mouseCanMove);
                if (ct.mouseCanMove)
                {
                    float v = dir.y;
                    var toward = Camera.main.transform.forward;
                    rig.linearVelocity = Vector2.zero;
                    rig.AddForce(500 * v * toward);
                }

            },
            OnStop = (Rigidbody rig) =>
            {
                rig.linearVelocity = Vector3.zero;
            }
        });
        cMvMd = moveModes["CameraFrontDirectionMove"];

        Tick.Reg(new() { offset = 1, onTick = (TickReg reg) => {
            if(ct.playerCanMove)
                OnMove(ct.wasdDirection);
            else
                OnStop(ct.wasdDirection);
            Tick.Reg(reg);
        } });

        SMesh.CreatePolygonMesh(new List<Vector3>()
        {
            new Vector3(0,0,0),
            new Vector3(1,0,0),
            new Vector3(1,1,0),
            new Vector3(0,1,0)
        });
        ct.bodies.LoadStruct(new() {location = Loc.zero,type = "test/normalCube" });
    }
    private void Update()
    {
        ct.pp = transform.position;// update player position
    }

    public void OnMove(Vector2 dir)
    {
        Tick.Reg(new() { offset = 1, onTick = (TickReg reg) => {
            cMvMd.OnMove(dir, entity.rig); 
        } });
    }
    public void OnStop(Vector2 dir)
    {
        Tick.Reg(new()
        {
            offset = 1,
            onTick = (TickReg reg) => {
                cMvMd.OnStop(entity.rig);
            }
        });
    }

    /// <summary>
    /// All registered move modes.
    /// 所有已注册的移动模式。
    /// </summary>
    public static Dictionary<string,MoveMode> moveModes = new();

}
public class MoveMode
{
    public delegate void Move(Vector2 dir, Rigidbody rig);
    public delegate void Stop(Rigidbody rig);

    /// <summary>
    /// can read every tick
    /// </summary>
    public Move OnMove;
    public Stop OnStop;
    public string Name;
}