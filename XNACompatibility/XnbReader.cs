#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ShaderDecompiler.XNACompatibility {
	public static class XnbReader {

		public static byte[] XnbMagic = Encoding.ASCII.GetBytes("XNB");

		public static bool CheckHeader(BinaryReader reader) {
			for (int i = 0; i < XnbMagic.Length; i++) {
				if (reader.ReadByte() != XnbMagic[i])
					return false;
			}
			return true;
		}

		public static Effect ReadEffect(BinaryReader reader) {

			if (!CheckHeader(reader))
				throw new Exception("Not xnb data");

			if (reader.ReadByte() != 119)
				throw new Exception("Bad xnb platform");
			
			ushort header = reader.ReadUInt16();
			bool compressed = (header & 0x8000) != 0;

			int length = reader.ReadInt32();

// https://github.com/FNA-XNA/FNA/blob/30cba4c463fab525843a86ffb7f2b4222a80410e/src/Content/ContentManager.cs#L497
			if (compressed) {

				int compressedSize = length - 14;
				int decompressedSize = reader.ReadInt32();
				MemoryStream decompressedStream = new MemoryStream(new byte[decompressedSize], 0, decompressedSize, true, true);
				MemoryStream compressedStream = new MemoryStream(reader.ReadBytes(compressedSize));
				LzxDecoder dec = new LzxDecoder(16);
				int decodedBytes = 0;
				long pos = 0L;
				while (pos < compressedSize) {
					int num = compressedStream.ReadByte();
					int lo = compressedStream.ReadByte();
					int block_size = num << 8 | lo;
					int frame_size = 32768;
					if (num == 255) {
						int num2 = lo;
						lo = (byte)compressedStream.ReadByte();
						frame_size = (num2 << 8 | lo);
						int num3 = (byte)compressedStream.ReadByte();
						lo = (byte)compressedStream.ReadByte();
						block_size = (num3 << 8 | lo);
						pos += 5L;
					}
					else {
						pos += 2L;
					}
					if (block_size == 0 || frame_size == 0) {
						break;
					}
					dec.Decompress(compressedStream, block_size, decompressedStream, frame_size);
					pos += block_size;
					decodedBytes += frame_size;
					compressedStream.Seek(pos, SeekOrigin.Begin);
				}
				if (decompressedStream.Position != decompressedSize) {
					throw new Exception("Decompression of xnb content failed. ");
				}
				decompressedStream.Seek(0L, SeekOrigin.Begin);
				compressedStream.Dispose();

				reader = new(decompressedStream);
			}

			int readerCount = reader.Read7BitEncodedInt();
			string[] readerTypes = new string[readerCount];
			for (int i = 0; i < readerCount; i++) {
				readerTypes[i] = reader.ReadString();
				reader.ReadInt32();
			}
			reader.Read7BitEncodedInt();

			int readerIndex = reader.Read7BitEncodedInt();

			if (readerIndex > readerCount || readerIndex < 1)
				throw new Exception("Unknown content type");

			if (!readerTypes[readerIndex-1].Contains("EffectReader"))
				throw new Exception("Not an xnb effect");

			int effectLength = reader.ReadInt32();
			return Effect.Read(reader);
		}
	}
}
