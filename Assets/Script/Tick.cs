using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

public class Tick : MonoBehaviour
{
    public TimeWheel wheel;
    public static Tick tickS;
    public int tps;
    public float t;

    private void Start()
    {
        tickS = this;

        tps = ct.setting.tickPerSecond;
        t = 1f / tps;
        wheel = new TimeWheel(tps);

        Cor(routine());
    }

    public static void Reg(Action<TimeWheel.TimerTask> action, int delay, int loop = 0, int interval = 0)
        => tickS.wheel.Add(action, delay, loop, interval);

    IEnumerator routine()
    {
        while (true)
        {
            wheel.Tick();
            yield return new WaitForSeconds(t);
        }
    }

    public static Coroutine Cor(IEnumerator routine)
    {
        return tickS.StartCoroutine(routine);
    }
    public static void StopCor(Coroutine routine)
    {
        tickS.StopCoroutine(routine);
    }
}

public class TimeWheel
{
    public class TimerTask
    {
        public int round;
        public int interval;
        public int loop; // 0=不重复，>0=剩余次数，-1=无限
        public Action<TimerTask> action;

        public void Stop()
        {
            loop = 0;
        }
    }

    private readonly List<TimerTask>[] _wheel;
    private readonly int _size;
    private int _current;

    public int getTick_ => _current;

    public TimeWheel(int size)
    {
        _size = size;
        _wheel = new List<TimerTask>[size];

        for (int i = 0; i < size; i++)
            _wheel[i] = new List<TimerTask>(4);
    }

    // ================= 添加任务 =================

    public void Add([CanBeNull] Action<TimerTask> action, int delay, int loop = 0, int interval = 0)
    {
        var task = new TimerTask
        {
            action = action,
            loop = loop,
            interval = interval > 0 ? interval : delay
        };

        Schedule(task, delay);
    }

    private void Schedule(TimerTask task, int delay)
    {
        int index = (_current + delay) % _size;
        int round = delay / _size;

        task.round = round;

        _wheel[index].Add(task);
    }

    // ================= Tick =================

    public void Tick()
    {
        var list = _wheel[_current];

        for (int i = 0; i < list.Count; i++)
        {
            var task = list[i];

            if (task.round > 0)
            {
                task.round--;
                continue;
            }

            // 执行
            task.action?.Invoke(task);

            // 从当前槽移除
            list.RemoveAt(i);
            i--;

            // ================= 重复逻辑 =================

            if (task.loop == 0)
            {
                // 不再重复，直接结束
                continue;
            }

            if (task.loop > 0)
            {
                task.loop--; // 执行后减少次数
            }

            // loop == -1 或 loop > 0 都会走到这里
            Schedule(task, task.interval);
        }

        _current = (_current + 1) % _size;
    }
}