using System.Collections.Generic;
using UnityEngine;

public class ChunkLoader
{
    public SPool<GameObject> structPool;
    /// <summary>
    /// info of loaded chunk
    /// </summary>
    public Dictionary<V3I, Chunk> loaded = new();
    public int loadradius;
    /// <summary>
    /// the center chunk's absolutely chunk position
    /// </summary>
    public V3I center;

    public ChunkLoader()
    {
        ct.log.Write("ChunkLoader","Load a chunk loader");
        structPool = new SPool<GameObject>(ct.setting.objectpPoolSize);
    }

    public void LoadChunk(Chunk chunk)
    {

    }

    public void LoadChunk(V3I cp) => LoadChunk(GetChunk(cp));



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