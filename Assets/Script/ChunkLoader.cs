using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChunkLoader
{
    public SPool<GameObject> structPool;
    /// <summary>
    /// info of loaded chunk
    /// </summary>
    public Dictionary<V3I, Chunk> loaded = new();
    public List<ChunkGenerator> generators = new();
    public int loadradius;
    /// <summary>
    /// the center chunk's absolutely chunk position
    /// </summary>
    public V3I center;
    public Bodies bodies;
    public Tick tickstm;

    public ChunkLoader(Bodies bodies)
    {
        ct.log.Write("ChunkLoader","Load a chunk loader");
        structPool = new SPool<GameObject>(ct.setting.objectpPoolSize);
        structPool.CreateNew = () => null;//can be create before, in bodies.LoadStruct
        structPool.OnInPool = o => { o.SetActive(false); };
        structPool.OnOutPool = o => { o.SetActive(true); };

        this.bodies = bodies;
    }

    public void LoadChunk(Chunk chunk)
    {
        var pertick = SMath.SliptList(chunk.structs,ct.setting.loadobjectsPerTick);

        foreach (var list in pertick)
        {
            TickReg reg = new TickReg()
            {
                onTick = (TickReg _) =>
                {
                    foreach (var state in list)
                    {
                        bodies.LoadStruct(state,strobj:structPool. Get());
                    }
                }
            };
            Tick.Reg(reg);
        }
    }
    public void LoadChunk(V3I cp) => LoadChunk(GetChunk(cp));

    public Chunk GenerateChunk(Chunk chunk)
    {
        return generators.Aggregate(chunk, (current, gt) => gt.Invoke(current));
    }

    /// <summary>
    /// load chunks in scene and remove other that out the range
    /// </summary>
    public void Loader(V3I center, int radius)
    {
        var removed = loaded.Keys;

        for (int ocx = -radius; ocx <= radius; ocx++)//offset chunk x
        {
            for (int ocy = -radius; ocy <= radius; ocy++)//offset chunk y
            {
                for (int ocz = -radius; ocy <= radius; ocy++)//offset chunk z
                {
                    var cur = center.Addition(ocx, ocy, ocz);//get offset

                    if (loaded.ContainsKey(cur))
                    {
                        return;
                    }
                    var ch = GetChunk(cur);
                    if (ch.isNull)//when does not exist chunk data
                    {
                        ch = GenerateChunk(ch);
                    }
                    loaded.Add(cur, ch);
                    LoadChunk(ch);
                }
            }
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
        T obj = pool.Count == 0 ? CreateNew() : pool.Dequeue();
        OnOutPool?.Invoke(obj);
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
public delegate Chunk ChunkGenerator(Chunk chunk);