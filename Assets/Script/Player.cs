using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public  bool moving = false;
    public Entity entity;
    /// <summary>
    /// current Move Mode
    /// </summary>
    public MoveMode cMvMd;

    private void Start()
    {
        ct.plyComp = this;
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
                float v = dir.y;
                var toward = Camera.main.transform.forward;
                rig.linearVelocity = Vector2.zero;
                rig.AddForce(500 * v * toward);
            },
            OnStop = (Rigidbody rig) =>
            {
                rig.linearVelocity = Vector3.zero;
            }
        });
        cMvMd = moveModes["CameraFrontDirectionMove"];

        Tick.Reg(new() { offset = 1, onTick = (TickReg reg) => {
            if (ct.playerMove.loosing)
            {
                if(moving)
                    OnMove(ct.wasdDirection);
                else
                    OnStop(ct.wasdDirection);
            }
            Tick.Reg(reg);
        } });

        SMesh.CreatePolygonMesh(new List<Vector3>()
        {
            new Vector3(0,0,0),
            new Vector3(1,0,0),
            new Vector3(1,1,0),
            new Vector3(0,1,0)
        });

        ct.mousecast.InCast += (GameObject o) =>
        {
            if(!o) return;
            if(o.CompareTag("struct") || o.CompareTag("structFace"))
                Bodies.OutlineObj(o, true);
        };
        ct.mousecast.OutCast += o =>
        {
            if(!o) return;
            if(o.CompareTag("struct") || o.CompareTag("structFace"))
                Bodies.OutlineObj(o, false);
        };
    }
    private void FixedUpdate()
    {
        var p =ct.pp = transform.position;// update player position
        ct.intPlayerPosition.Set(p);
        ct.pcp = ct.pip / ct.setting.chunkUnit;
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
    /// ������ע����ƶ�ģʽ��
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