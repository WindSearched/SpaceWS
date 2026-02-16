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
                    if (reg.repeattime >= 1)
                        Reg(reg);

                    else if(reg.repeattime == -100)
                        Reg(reg, tick + 1 + reg.interval);
                }
            }

            tickEvents.Remove(tick);
            tick++;
        }
    }

    public static void Reg(TickReg reg)
    {
        int t = ticksym.tick + reg.offset;
        Reg(reg,t);
    }

    public static void Reg(TickReg reg, int tick)
    {
        while (true)
        {
            if (tick < ticksym.tick) return;
            if (ticksym.tickEvents.ContainsKey(tick))
            {
                ticksym.tickEvents[tick].Add(reg);
            }
            else
            {
                ticksym.tickEvents[tick] = new List<TickReg>() { reg };
            }

            if (reg.repeattime <= 0) return;
            reg.repeattime--;
            var reg1 = reg;
            tick = tick + reg1.interval;
        }
    }

    public static void Reg(TickEv ev, int offset, int repeat = 0, int interval = 0)
    {
        Reg(new(ev, offset, repeat, interval));
    }

    /// <summary>
    /// register batch with a interval
    /// </summary>
    /// <param name="regs"></param>
    /// <param name="interval"></param>
    public static void Reg(List<TickReg> regs, int interval)
    {
        int curoffset = 0;
        foreach (var reg in regs)
        {
            Reg(new ()
            {
                onTick =  reg.onTick,
                repeattime = reg.repeattime,
                offset = reg.offset + curoffset
            });
            curoffset += interval;
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
    public int interval;

    public TickReg(TickEv onTick, int offset, int repeat = 0, int interval = 0)
    {
        this.onTick = onTick;
        this.offset = offset;
        this.repeattime = repeat;
        this.interval = interval;
    }
}

public delegate void ObjEv(Object obj);
public delegate void TransfEv(Transform transform);