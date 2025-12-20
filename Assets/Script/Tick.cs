using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tick : MonoBehaviour
{
    public static Tick ticksym;
    public int tickPerSecond = 5;
    public int tick;
    public float tickInterval;
    public Dictionary<int, List<TickReg>> tickEvents = new();
    private void Start()
    {
        ticksym = this;
        tickInterval = 1f / tickPerSecond;

        StartCoroutine(TickRoutine());

        TickEv e = (TickReg reg) =>
        {
            Reg(reg);
        };
        TickReg reg = new(e,5, 0);
        Reg(reg);
    }

    // ReSharper disable Unity.PerformanceAnalysis
    IEnumerator TickRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(tickInterval);

            if(tickEvents.TryGetValue(tick, out List<TickReg> regs))//try get tick events
            {
                foreach (var t in regs)
                {
                    var reg = t;
                    reg.onTick.Invoke(reg);

                    reg.repeattime--;
                    if (reg.repeattime < 1) continue;
                    Reg(reg);
                }
            }

            tickEvents.Remove(tick);
            tick++;
        }
    }

    public static void Reg(TickReg reg)
    {
        int t = ticksym.tick + reg.offset;
        if (ticksym.tickEvents.ContainsKey(t))
        {
            ticksym.tickEvents[t].Add(reg);
        }
        else
        {
            ticksym.tickEvents[t] = new List<TickReg>() { reg };
        }
    }

    public static Coroutine Cor(IEnumerator routine)
    {
        return ticksym.StartCoroutine(routine);
    }
    public static void StopCor(Coroutine routine)
    {
        ticksym.StopCoroutine(routine);
    }
}
public delegate void TickEv(TickReg reg);
/// <summary>
/// tick event register
/// </summary>
public struct TickReg
{
    public TickEv onTick;
    public int offset;
    public int repeattime;

    public TickReg(TickEv onTick, int offset, int repeat = 0)
    {
        this.onTick = onTick;
        this.offset = offset;
        this.repeattime = repeat;
    }
}

public delegate void ObjEv(Object obj);
public delegate void TransfEv(Transform transform);