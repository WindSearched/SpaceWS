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
        TickReg reg = new(e,5, true);
        Reg(reg);
    }

    IEnumerator TickRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(tickInterval);

            if(tickEvents.TryGetValue(tick, out List<TickReg> regs))//try get tick events
            {
                for (int i = 0; i < regs.Count; i++)
                {
                    regs[i].onTick.Invoke(regs[i]);
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
    public bool repeat;

    public TickReg(TickEv onTick, int offset, bool repeat)
    {
        this.onTick = onTick;
        this.offset = offset;
        this.repeat = repeat;
    }
}

public delegate void ObjEv(Object obj);
public delegate void TransfEv(Transform transform);