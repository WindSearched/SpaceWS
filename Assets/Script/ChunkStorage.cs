using K4os.Compression.LZ4;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class ChunkStorage
{
    // =========================
    // 配置
    // =========================

    public const int REGION_SIZE = 16;     // 16x16x16 chunks
    public const int PAGE_SIZE = 4096;     // segment 页大小

    // =========================
    // 内部结构
    // =========================

    struct Entry
    {
        public bool exists;
        public int segment;
        public int offset;
        public int length;
    }

    class Region
    {
        public string path;
        public Entry[] entries;
        public Dictionary<int, FileStream> segments = new();

        public Region(string path)
        {
            this.path = path;
            entries = new Entry[REGION_SIZE * REGION_SIZE * REGION_SIZE];
            LoadIndex();
        }

        string IndexPath => path + ".index";

        void LoadIndex()
        {
            if (!File.Exists(IndexPath)) return;

            using var br = new BinaryReader(File.OpenRead(IndexPath));
            for (int i = 0; i < entries.Length; i++)
            {
                entries[i].exists = br.ReadBoolean();
                entries[i].segment = br.ReadInt32();
                entries[i].offset = br.ReadInt32();
                entries[i].length = br.ReadInt32();
            }
        }

        void SaveIndex()
        {
            using var bw = new BinaryWriter(File.Create(IndexPath));
            for (int i = 0; i < entries.Length; i++)
            {
                bw.Write(entries[i].exists);
                bw.Write(entries[i].segment);
                bw.Write(entries[i].offset);
                bw.Write(entries[i].length);
            }
        }

        FileStream GetSegment(int id)
        {
            if (!segments.TryGetValue(id, out var fs))
            {
                fs = new FileStream(path + $".seg{id}", FileMode.OpenOrCreate, FileAccess.ReadWrite);
                segments[id] = fs;
            }
            return fs;
        }

        public void Save(int index, byte[] data)
        {
            int segId = 0;
            FileStream fs;

            while (true)
            {
                fs = GetSegment(segId);
                if (fs.Length + data.Length < int.MaxValue) break;
                segId++;
            }

            int offset = (int)fs.Length;
            fs.Seek(offset, SeekOrigin.Begin);
            fs.Write(data, 0, data.Length);

            entries[index] = new Entry
            {
                exists = true,
                segment = segId,
                offset = offset,
                length = data.Length
            };

            SaveIndex();
        }

        public byte[] Load(int index)
        {
            if (!entries[index].exists) return null;

            var e = entries[index];
            var fs = GetSegment(e.segment);

            byte[] data = new byte[e.length];
            fs.Seek(e.offset, SeekOrigin.Begin);
            fs.Read(data, 0, e.length);
            return data;
        }

        public void Delete(int index)
        {
            entries[index].exists = false;
            SaveIndex();
        }
    }

    // =========================
    // Region 缓存
    // =========================

    static readonly Dictionary<string, Region> regions = new();

    // =========================
    // 对外 API
    // =========================

    public static void SaveChunk(string world, int cx, int cy, int cz, byte[] data)
    {
        var (region, index) = GetRegionAndIndex(world, cx, cy, cz);
        byte[] compressed = ChunkCompressor.Compress(data);
        region.Save(index, compressed);
    }
    public static void SaveChunk(string world, V3I cp, byte[] data)
    {
        SaveChunk(world, cp.x, cp.y, cp.z, data);
    }
    public static byte[] LoadChunk(string world, int cx, int cy, int cz)
    {
        var (region, index) = GetRegionAndIndex(world, cx, cy, cz);
        byte[] compressed = region.Load(index);
        return compressed == null ? null : ChunkCompressor.Decompress(compressed);
    }
    public static byte[] LoadChunk(string world, V3I cp)
    {
        return LoadChunk(world, cp.x, cp.y, cp.z);
    }

    public static void DeleteChunk(string world, int cx, int cy, int cz)
    {
        var (region, index) = GetRegionAndIndex(world, cx, cy, cz);
        region.Delete(index);
    }

    // =========================
    // 坐标 & Region 解析
    // =========================

    static (Region region, int index) GetRegionAndIndex(string world, int cx, int cy, int cz)
    {
        int rx = FloorDiv(cx, REGION_SIZE);
        int ry = FloorDiv(cy, REGION_SIZE);
        int rz = FloorDiv(cz, REGION_SIZE);

        int lx = cx - rx * REGION_SIZE;
        int ly = cy - ry * REGION_SIZE;
        int lz = cz - rz * REGION_SIZE;

        int index =
            ly * REGION_SIZE * REGION_SIZE +
            lz * REGION_SIZE +
            lx;

        string key = $"{world}:{rx}:{ry}:{rz}";

        if (!regions.TryGetValue(key, out var region))
        {
            string dir = Path.Combine(ct.setting.spacePath, world);
            Directory.CreateDirectory(dir);

            string basePath = Path.Combine(dir, $"r.{rx}.{ry}.{rz}");
            region = new Region(basePath);
            regions[key] = region;
        }

        return (region, index);
    }

    static int FloorDiv(int a, int b)
    {
        return (a >= 0) ? (a / b) : ((a - b + 1) / b);
    }
}


public static class ChunkCompressor
{
    // 压缩格式：
    // [int rawSize][int compressedSize][compressedBytes...]

    public static byte[] Compress(byte[] raw)
    {
        int max = LZ4Codec.MaximumOutputSize(raw.Length);
        byte[] buffer = new byte[8 + max];

        // 写原始大小
        BitConverter.GetBytes(raw.Length).CopyTo(buffer, 0);

        int compSize = LZ4Codec.Encode(
            raw, 0, raw.Length,
            buffer, 8, max
        );

        // 写压缩后大小
        BitConverter.GetBytes(compSize).CopyTo(buffer, 4);

        Array.Resize(ref buffer, 8 + compSize);
        return buffer;
    }

    public static byte[] Decompress(byte[] data)
    {
        int rawSize = BitConverter.ToInt32(data, 0);
        int compSize = BitConverter.ToInt32(data, 4);

        byte[] raw = new byte[rawSize];
        LZ4Codec.Decode(
            data, 8, compSize,
            raw, 0, rawSize
        );
        return raw;
    }
}