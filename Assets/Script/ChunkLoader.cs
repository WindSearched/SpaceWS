using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChunkLoader
{
    public SPool<GameObject> structPool;
    public GameObject poolParent;
    /// <summary>
    /// info of loaded chunk
    /// </summary>
    public Dictionary<V3I, Chunk> loaded = new();
    public List<ChunkGenerator> generators = new();
    /// <summary>
    /// the center chunk's absolutely chunk position
    /// </summary>
    public Bodies bodies;

    public ChunkLoader(Bodies bodies)
    {
        ct.log.Write("ChunkLoader","Load a chunk loader");
        poolParent = new GameObject("structpool");
        structPool = new SPool<GameObject>(ct.setting.objectpPoolSize);
        structPool.CreateNew = () => null;//can be create before, in bodies.LoadStruct
        structPool.OnInPool = o =>
        {
            o.SetActive(false);
            o.transform.SetParent(poolParent.transform);
        };
        structPool.OnOutPool = o => { o.SetActive(true); };

        this.bodies = bodies;
    }

    public void LoadChunk(Chunk chunk)
    {
        int of = 0;

        var bodytPT = SMath.SliptList(chunk.bodies,ct.setting.loadobjectsPerTick);
        foreach (var list in bodytPT)
        {
            Tick.Reg(new (_ =>
                {
                    foreach (var s in list)
                    {
                        bodies.LoadVoidBody(s.index,s.location,chunk.position);
                    }
                },
                of++
            ));
        }

        var structPT = SMath.SliptList(chunk.structs,ct.setting.loadobjectsPerTick);
        foreach (var list in structPT)
        {
            TickReg reg = new TickReg()
            {
                onTick = (TickReg _) =>
                {
                    foreach (var state in list)
                    {
                        bodies.LoadStruct(state, chunk.position ,strobj:structPool. Get());
                    }
                },
                offset = of++
            };
            Tick.Reg(reg);
        }
    }
    public void LoadChunk(V3I cp) => LoadChunk(GetChunk(cp));

    public Chunk GenerateChunk(V3I cp)
    {
        Chunk c = new(cp);
        foreach (var g in generators)
        {
            g?.Invoke(c);
        }
        return c;
    }

    public void RemoveChunk(Chunk chunk)
    {
        foreach (var s in chunk.structs)//remove structs of chunk
        {
            var idx = s.bodyIndex;
            var g = bodies.objects[idx];
            structPool.Put(g.self);
        }
    }

    /// <summary>
    /// load chunks in scene and remove other that out the range
    /// </summary>
    public void Loader(V3I center, int radius)
    {
        var removed = loaded.Keys.ToList();

        for (int ocx = -radius; ocx <= radius; ocx++)//offset chunk x
        {
            for (int ocy = -radius; ocy <= radius; ocy++)//offset chunk y
            {
                for (int ocz = -radius; ocz <= radius; ocz++)//offset chunk z
                {
                    var cur = center.Addition(ocx, ocy, ocz);//get offset

                    if (loaded.ContainsKey(cur))
                    {
                        Debug.Log("existed");
                        removed.Remove(cur);
                        continue;
                    }
                    var ch = GetChunk(cur);
                    if (ch == null)//when does not exist chunk data
                    {
                        Debug.Log("null");
                        ch = GenerateChunk(cur);
                    }
                    loaded.Add(cur, ch);
                    LoadChunk(ch);
                }
            }
        }

        foreach (var r in removed)
        {
            RemoveChunk(loaded[r]);
        }
    }


    public Chunk GetChunk(V3I cp)
    {
        return Chunk.FromBytes(ChunkStorage.LoadChunk(ct.curWorldRule.name, cp));
    }
}

public class SPool<T>
{
    public int poolsize;
    public Queue<T> pool = new();
    public PoolMeth<T> OnInPool;
    public PoolMeth<T> OnOutPool;
    public PoolMethR<T> CreateNew;

    public SPool(int size = 512)
    {
        ct.log.Write("SPool",$"Load a {nameof(T)} pool with size {size}");
        poolsize = size;
    }

    T New() => CreateNew();
    public T Get()
    {
        T obj;
        if (pool.Count == 0)
        {
            obj = CreateNew();
        }
        else
        {
            obj = pool.Dequeue();
            OnOutPool?.Invoke(obj);
        }
        return obj;
    }

    public void Put(T obj)
    {
        OnInPool?.Invoke(obj);
        if (pool.Count > poolsize)
            return;
        pool.Enqueue(obj);
    }
}

public delegate void PoolMeth<in T>(T obj);
public delegate T PoolMethR<T>();
public delegate void ChunkGenerator(Chunk chunk);