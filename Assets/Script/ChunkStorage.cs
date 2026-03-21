using System;
using System.Collections.Generic;
using System.IO;
using K4os.Compression.LZ4;
using MemoryPack;

public static class ChunkStorage
{
	public const int PAGE_SIZE = 4096; // segment ҳ��С

	// =========================
	// Region ����
	// =========================

	private static readonly Dictionary<string, Region> regions = new();
	// =========================
	// ����
	// =========================

	public static int REGION_SIZE => ct.setting.regionSize; // 16x16x16 chunks

	// =========================
	// ���� API
	// =========================

	public static void SaveChunk(string world, int cx, int cy, int cz, byte[] data)
	{
		var (region, index) = GetRegionAndIndex(world, cx, cy, cz);
		var compressed = ChunkCompressor.Compress(data);
		region.Save(index, compressed);
	}

	public static void SaveChunk(string world, V3I cp, byte[] data)
	{
		SaveChunk(world, cp.x, cp.y, cp.z, data);
	}

	public static byte[] LoadChunk(string world, int cx, int cy, int cz)
	{
		var (region, index) = GetRegionAndIndex(world, cx, cy, cz);
		var compressed = region.Load(index);
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
	// ���� & Region ����
	// =========================

	private static (Region region, int index) GetRegionAndIndex(string world, int cx, int cy, int cz)
	{
		var rx = FloorDiv(cx, REGION_SIZE);
		var ry = FloorDiv(cy, REGION_SIZE);
		var rz = FloorDiv(cz, REGION_SIZE);

		var lx = cx - rx * REGION_SIZE;
		var ly = cy - ry * REGION_SIZE;
		var lz = cz - rz * REGION_SIZE;

		var index =
			ly * REGION_SIZE * REGION_SIZE +
			lz * REGION_SIZE +
			lx;

		var key = $"{world}:{rx}:{ry}:{rz}";

		if (!regions.TryGetValue(key, out var region))
		{
			var dir = Path.Combine(ct.setting.spacePath, world);
			Directory.CreateDirectory(dir);

			var basePath = Path.Combine(dir, $"r.{rx}.{ry}.{rz}");
			region = new Region(basePath);
			regions[key] = region;
		}

		return (region, index);
	}

	private static int FloorDiv(int a, int b)
	{
		return a >= 0 ? a / b : (a - b + 1) / b;
	}

	// =========================
	// �ڲ��ṹ
	// =========================

	private struct Entry
	{
		public bool exists;
		public int segment;
		public int offset;
		public int length;
	}

	private class Region
	{
		public readonly Entry[] entries;
		public readonly string path;
		public readonly Dictionary<int, FileStream> segments = new();

		public Region(string path)
		{
			this.path = path;
			entries = new Entry[REGION_SIZE * REGION_SIZE * REGION_SIZE];
			LoadIndex();
		}

		private string IndexPath => path + ".index";

		private void LoadIndex()
		{
			if (!File.Exists(IndexPath)) return;

			using var br = new BinaryReader(File.OpenRead(IndexPath));
			for (var i = 0; i < entries.Length; i++)
			{
				entries[i].exists = br.ReadBoolean();
				entries[i].segment = br.ReadInt32();
				entries[i].offset = br.ReadInt32();
				entries[i].length = br.ReadInt32();
			}
		}

		private void SaveIndex()
		{
			using var bw = new BinaryWriter(File.Create(IndexPath));
			for (var i = 0; i < entries.Length; i++)
			{
				bw.Write(entries[i].exists);
				bw.Write(entries[i].segment);
				bw.Write(entries[i].offset);
				bw.Write(entries[i].length);
			}
		}

		private FileStream GetSegment(int id)
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
			var segId = 0;
			FileStream fs;

			while (true)
			{
				fs = GetSegment(segId);
				if (fs.Length + data.Length < int.MaxValue) break;
				segId++;
			}

			var offset = (int)fs.Length;
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

			var data = new byte[e.length];
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
}


public static class ChunkCompressor
{
	// ѹ����ʽ��
	// [int rawSize][int compressedSize][compressedBytes...]

	public static byte[] Compress(byte[] raw)
	{
		var max = LZ4Codec.MaximumOutputSize(raw.Length);
		var buffer = new byte[8 + max];

		// дԭʼ��С
		BitConverter.GetBytes(raw.Length).CopyTo(buffer, 0);

		var compSize = LZ4Codec.Encode(
			raw, 0, raw.Length,
			buffer, 8, max
		);

		// дѹ�����С
		BitConverter.GetBytes(compSize).CopyTo(buffer, 4);

		Array.Resize(ref buffer, 8 + compSize);
		return buffer;
	}

	public static byte[] Decompress(byte[] data)
	{
		var rawSize = BitConverter.ToInt32(data, 0);
		var compSize = BitConverter.ToInt32(data, 4);

		var raw = new byte[rawSize];
		LZ4Codec.Decode(
			data, 8, compSize,
			raw, 0, rawSize
		);
		return raw;
	}
}