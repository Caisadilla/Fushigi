﻿using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;

namespace Fushigi.Msbt
{
    public class MsbtFile
    {
        [StructLayout(LayoutKind.Sequential, Size = 32)]
        public struct MsbtHeader
        {
            public ulong Magic; //MsgStdBn
            public ushort BOM; //FFFE
            public ushort Padding;

            public byte Encoding;
            public byte Version; //3
            public ushort NumSections;
            public ushort Padding2;

            public ulong Padding3;
            public uint Padding4;
        }

        public Dictionary<string, string> Messages = new Dictionary<string, string>();

        private byte[] AttributeData;

        private MsbtHeader Header;

        public MsbtFile(string filePath) {
            Read(File.OpenRead(filePath));
        }

        public MsbtFile(Stream stream) {
            Read(stream);
        }

        public void Read(Stream stream)
        {
            stream.Read(Utils.AsSpan(ref Header));

            stream.Position = 0;

            var encoding = Header.Encoding == 0 ? Encoding.UTF8 : Encoding.Unicode;
            var reader = new BinaryReader(stream, encoding);

            reader.BaseStream.Seek(32, SeekOrigin.Begin);

            Dictionary<string, uint> labels = new();
            string[] messages = new string[0];

            for (int i = 0; i < Header.NumSections; i++)
            {
                string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
                uint size = reader.ReadUInt32();
                reader.ReadBytes(8); //padding

                var pos = reader.BaseStream.Position;

                switch (magic)
                {
                    case "LBL1": labels = ReadLabel(reader); break;
                    case "ATR1": AttributeData = reader.ReadBytes((int)size); break;
                    case "TXT2": messages = ReadText2(reader, encoding); break;
                }

                reader.BaseStream.Seek(pos + size, SeekOrigin.Begin);
                reader.Align(16);
            }

            if (labels != null)
            {
                foreach (var label in labels)
                    this.Messages.Add(label.Key, messages[label.Value]);
            }
            else
            {
                for (int i = 0; i < messages.Length; i++)
                    this.Messages.Add(i.ToString(), messages[i]);
            }
        }

        public void Write(Stream stream)
        {
            var encoding = Header.Encoding == 0 ? Encoding.UTF8 : Encoding.Unicode;

            var writer = new BinaryWriter(stream, encoding);
            writer.Write(Utils.AsSpan(ref Header));

            writer.BaseStream.Seek(32, SeekOrigin.Begin);
            WriteSection(writer, "LBL1", () => WriteLabel(writer, Messages.Keys.ToArray()));
            WriteSection(writer, "ATR1", () => WriteAttribute(writer));
            WriteSection(writer, "TXT2", () => WriteText2(writer, Messages.Values.ToArray()));
        }

        private void WriteSection(BinaryWriter writer, string magic, Action sectionWriter)
        {
            writer.Write(Encoding.ASCII.GetBytes(magic));
            writer.Write(0); //size for later
            writer.Write(new byte[12]); //padding for alignment

            sectionWriter.Invoke();

            writer.Align(16);
        }

        private void ReadAttribute(BinaryReader reader)
        {

        }

        private Dictionary<string, uint> ReadLabel(BinaryReader reader)
        {
            Dictionary<string, uint> labels = new Dictionary<string, uint>();

            long startPosition = reader.BaseStream.Position;

            uint num_hash_slots = reader.ReadUInt32();
            for (uint i = 0; i < num_hash_slots; i++)
            {
                reader.BaseStream.Seek(startPosition + 4 + (i * 8), SeekOrigin.Begin);

                uint numEntries = reader.ReadUInt32();
                uint offset = reader.ReadUInt32();

                reader.BaseStream.Seek(startPosition + offset, SeekOrigin.Begin);
                for (int j = 0; j < numEntries; j++)
                {
                    byte strLength = reader.ReadByte();
                    string label = Encoding.UTF8.GetString(reader.ReadBytes(strLength));
                    uint text_index = reader.ReadUInt32();

                    labels.Add(label, text_index); 
                }
            }
            return labels;
        }

        private string[] ReadText2(BinaryReader reader, Encoding encoding)
        {
            long startPosition = reader.BaseStream.Position;

            uint num = reader.ReadUInt32();
            uint[] offsets = Enumerable.Range(0, (int)num).Select(x => reader.ReadUInt32()).ToArray();

            string[] messages = new string[num];
            for (uint i = 0; i < num; i++)
            {
                reader.BaseStream.Seek(startPosition + offsets[i], SeekOrigin.Begin);
                messages[i] = ReadMessageText(reader, encoding);
            }
            return messages;
        }

        private string ReadMessageText(BinaryReader reader, Encoding encoding)
        {
            bool isNullTerimated = false;

            StringBuilder sb = new StringBuilder();

            while (!isNullTerimated)
            {
                char c = reader.ReadChar();
                sb.Append(c);

                switch ((int)c)
                {
                    case 0xE:
                        //tag code
                        sb.Append((char)reader.ReadInt16());
                        sb.Append((char)reader.ReadInt16());
                        //tag parameters
                        int count = reader.ReadInt16();
                        sb.Append((char)count);
                        for (int i = 0; i < count; i++)
                        {
                            sb.Append((char)reader.ReadByte());
                        }
                        break;
                    case 0x0F: //tag end
                        sb.Append((char)reader.ReadInt16());
                        sb.Append((char)reader.ReadInt16());
                        break;
                    case 0x00:
                        isNullTerimated = true;
                        break;
                }
            }
            return sb.ToString(); 
        }

        private void WriteLabel(BinaryWriter writer, string[] labels)
        {
            writer.Write(1);
            writer.Write((uint)labels.Length);
            writer.Write(0x0C);
            for (uint i = 0; i < labels.Length; i++)
            {
                writer.Write((byte)labels[i].Length);
                writer.Write(Encoding.ASCII.GetBytes(labels[i]));
                writer.Write(i);
            }
        }

        private void WriteAttribute(BinaryWriter writer)
        {
            writer.Write(AttributeData);
        }

        private void WriteText2(BinaryWriter writer, string[] text)
        {
            long startPosition = writer.BaseStream.Position;

            writer.Write(text.Length);
            for (int i = 0; i < text.Length; i++)
                writer.Write(0);

            for (int i = 0; i < text.Length; i++)
            {
                writer.WriteOffset(startPosition + 4 + i * 4, startPosition);
                for (int j = 0; j < text[i].Length; j++)
                    writer.Write((char)text[i][j]);

                writer.Write((char)0);
            }
        }
    }
}
