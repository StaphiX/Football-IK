using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class BinaryObjectData
{
    public BinaryObjectData(uint classHeaderBitmask, byte classMemberIndex, long objectWriteHeadrByteIndex)
    {
        this.classHeaderBitmask = classHeaderBitmask;
        this.classMemberIndex = classMemberIndex;
        this.objectWriteHeadrByteIndex = objectWriteHeadrByteIndex;
    }

    public uint classHeaderBitmask = 0;
    public byte classMemberIndex = 0;
    public long objectWriteHeadrByteIndex = 0;
}

public class BinarySerializer : FileSerializer, IDisposable
{
    // This is how we know which fields have been written to
    // Classes can not have more than 31 serialized members
    // A value of 0 is used to ignore the class alltogether
    Stack<BinaryObjectData> nestedObjectData = new Stack<BinaryObjectData>();

    private BinaryWriter writer = null;
    private BinaryReader reader = null;
    public bool isInitialised = false;
    public bool isClosed = false;

    const byte EndArrayToken = 0;
    const byte EndObjectToken = 0;

    public BinarySerializer(uint latestVersion, string filePath, bool isReading) : base(latestVersion, latestVersion, isReading)
    {
        try
        {
            if (isReading)
            {
                if (!FileSystem.Exists(filePath))
                {
                    return;
                }

                reader = new BinaryReader(File.Open(filePath, FileMode.Open));
                serializeVersion = ReadUInt();
                isInitialised = true;
            }
            else
            {
                FileSystem.CreateDirRecursive(Path.GetDirectoryName(filePath));
                writer = new BinaryWriter(File.Open(filePath, FileMode.OpenOrCreate));
                writer.Write(latestVersion);
                isInitialised = true;
            }
        }
        catch (Exception e)
        {
            Exception baseEx = e.GetBaseException();
            Debug.Log(baseEx.Message);
            Debug.Log(baseEx.StackTrace);

            Dispose();
        }
    }

    public BinarySerializer(uint latestVersion, byte[] bytes) : base(latestVersion, latestVersion, true)
    {
        try
        {
            MemoryStream memoryStream = new MemoryStream(bytes);
            reader = new BinaryReader(memoryStream);
            serializeVersion = ReadUInt();
            isInitialised = true;
        }
        catch (Exception e)
        {
            Exception baseEx = e.GetBaseException();
            Debug.Log(baseEx.Message);
            Debug.Log(baseEx.StackTrace);

            Dispose();
        }
    }

    ~BinarySerializer()
    {
        Dispose();
    }

    public void Dispose()
    {
        Finish();
        GC.SuppressFinalize(this);
    }

    private void Finish()
    {
        if (isClosed)
            return;

        isClosed = true;

        if (isReading)
        {
            if (reader == null)
                return;

            reader.Close();
            return;
        }

        if (writer == null)
            return;

        writer.Close();
    }

    public bool IsSerializingClass()
    {
        return nestedObjectData.Count > 0;
    }

    public void AddObjectData(uint classHeaderBitmask, byte classMemberIndex, long classHeaderByteIndex)
    {
        nestedObjectData.Push(new BinaryObjectData(classHeaderBitmask, classMemberIndex, classHeaderByteIndex));
    }

    public bool CheckClassBitmask<T>(ref T value, T defaultValue)
    {
        if (!IsSerializingClass())
            return true;

        if (!isReading)
        {
            BinaryObjectData writeData = nestedObjectData.Peek();

            // Write this it is not the default value
            bool isDefault = Equals(value, defaultValue);
            if (!isDefault)
            {
                writeData.classHeaderBitmask |= ((uint)1 << writeData.classMemberIndex);
            }

            ThrowException(writeData.classMemberIndex > 30, "This class has too many members to serialize correctly: combine members into objects");

            writeData.classMemberIndex += 1;

            return !isDefault;
        }

        // Reading the file
        BinaryObjectData readData = nestedObjectData.Peek();

        // Check the member index 0 - 31 against the serialized bitmask
        bool hasMember = (readData.classMemberIndex & (1 << (int)readData.classMemberIndex)) > 0;

        if(!hasMember)
        {
            value = defaultValue;
        }

        ThrowException(readData.classMemberIndex > 30, "This class has too many members to serialize correctly: combine members into objects");

        readData.classMemberIndex += 1;

        return hasMember;
    }

    public int ReadInt()
    {
        return reader.ReadInt32();
    }

    public float ReadFloat()
    {
        return reader.ReadSingle();
    }

    public uint ReadUInt()
    {
        return reader.ReadUInt32();
    }

    public ushort ReadUInt16()
    {
        return reader.ReadUInt16();
    }

    public byte ReadByte()
    {
        return reader.ReadByte();
    }

    public void ReadObjectHeader()
    {
        uint bitmask = ReadUInt();
        AddObjectData(bitmask, 0, 0);
    }

    public ushort ReadArrayHeader()
    {
        return ReadUInt16();
    }

    public byte ReadObjectEndToken()
    {
        if(IsSerializingClass())
            nestedObjectData.Pop();
        return reader.ReadByte();
    }

    public byte ReadArrayEndToken()
    {
        return reader.ReadByte();
    }

    public long Read7BitEncodedInt()
    {
        // Read out an Int32 7 bits at a time.  The high bit
        // of the byte when on means to continue reading more bytes.
        long count = 0;
        int shift = 0;
        byte b;
        do
        {
            // Check for a corrupted stream.  Read a max of 5 bytes.
            // In a future version, add a DataFormatException.
            // 5 bytes max per Int32, shift += 7
            ThrowException(shift == 5 * 7, "Corrupted 7 bit encoded calue can not be read");

            // ReadByte handles end of stream cases for us.
            b = ReadByte();
            count |= (long)(b & 0x7F) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);

        return count;
    }

    public void UpdateObjectHeader()
    {
        // Get the current write byte position
        BinaryObjectData writeData = nestedObjectData.Peek();
        long currentByteIndex = writer.Seek(0, SeekOrigin.Current);
        // Move to the byte position of the object header
        writer.Seek((int)writeData.objectWriteHeadrByteIndex, SeekOrigin.Begin);
        // Overwrite the object header
        writer.Write(writeData.classHeaderBitmask);

        // Move back to the current write position
        writer.Seek((int)currentByteIndex, SeekOrigin.Begin);

        DebugMsg(string.Format("Write Final Object Header: {0} at byte {1}", writeData.classHeaderBitmask, writeData.objectWriteHeadrByteIndex));
    }

    public void WriteStartObject()
    {
        long objectWriteHeadrByteIndex = writer.Seek(0, SeekOrigin.Current);
        AddObjectData(0, 0, objectWriteHeadrByteIndex);
        writer.Write(uint.MaxValue);

        DebugMsg(string.Format("Write Temp Object Header at byte: {0}", objectWriteHeadrByteIndex));
    }

    public void WriteCloseObject()
    {
        if(IsSerializingClass())
        {
            UpdateObjectHeader();
            nestedObjectData.Pop();
        }

        writer.Write(EndObjectToken);

        DebugMsg(string.Format("Write Object Closed Token {0}", EndObjectToken));
    }

    public void WriteArrayHeader(UInt16 arraySize)
    {
        writer.Write(arraySize);

        DebugMsg(string.Format("Write Array Size {0}", arraySize));
    }

    public void WriteArrayEndToken()
    {
        writer.Write(EndArrayToken);

        DebugMsg(string.Format("Write End Array Token {0}", EndArrayToken));
    }

    public void Write7BitEncodedInt(long value)
    {
        // Write out an int 7 bits at a time.  The high bit of the byte,
        // when on, tells reader to continue reading more bytes.
        ulong v = (ulong)value;   // support negative numbers
        while (v >= 0x80)
        {
            writer.Write((byte)(v | 0x80));
            v >>= 7;
        }
        writer.Write((byte)v);
    }

    public override void SetObject<T>(string name, ref T obj, CreateNew<T> createNew, GetType<T> getType = null, int firstVersion = -1, int lastVersion = -1)
    {
        bool validVersion = CheckVersion(ref obj, default, firstVersion, ref lastVersion);

        if (!validVersion)
            return;

        bool validClassMember = CheckClassBitmask(ref obj, default);
        if (!validClassMember)
            return;

        byte objectType = 0;
        if (isReading)
        {
            ReadObjectHeader();

            if (getType != null)
                objectType = ReadByte();

            obj = createNew(objectType);
        }

        if (!isReading)
        {
            WriteStartObject();
            if (getType != null)
            {
                objectType = (byte)getType(obj);
                writer.Write(objectType);
            }
        }

        obj.Serialize(this);

        if (isReading)
        {
            int objectToken = ReadObjectEndToken();
            ThrowException(objectToken != EndObjectToken, string.Format("Did not get end object token successfully: {0}", name));
        }
        else
        {
            WriteCloseObject();
        }
    }

    public override void SetObjectArray<T>(string name, ref List<T> list, CreateNew<T> createNew, GetType<T> getType = null, int firstVersion = -1, int lastVersion = -1)
    {
        bool validVersion = CheckVersion(ref list, null, firstVersion, ref lastVersion);

        if (!validVersion)
            return;

        bool validClassMember = CheckClassBitmask(ref list, null);
        if (!validClassMember)
            return;

        if (isReading && list == null)
            list = new List<T>();

        int listCount = list.Count;

        ThrowException(listCount > ushort.MaxValue, "Too many items to write in this array: max 65535");

        if (isReading)
        {
            listCount = ReadArrayHeader();
        }
        else
        {
            WriteArrayHeader((ushort)listCount);
        }

        for(int index = 0; index < listCount; ++index)
        {
            byte objectType = 0;
            if(isReading)
            {
                ReadObjectHeader();

                if (getType != null)
                    objectType = ReadByte();

                list.Add(createNew(objectType));
            }

            if (!isReading)
            {
                WriteStartObject();
                if (getType != null)
                {
                    objectType = (byte)getType(list[index]);
                    writer.Write(objectType);
                }
            }

            list[index].Serialize(this);

            if(isReading)
            {
                int objectToken = ReadObjectEndToken();
                ThrowException(objectToken != EndObjectToken, string.Format("Did not get end object token successfully: {0}", name));
            }
            else
            {
                WriteCloseObject();
            }
        }

        if(isReading)
        {
            int arrayToken = ReadArrayEndToken();
            ThrowException(arrayToken != EndArrayToken, string.Format("Did not get end array token successfully: {0}", name));
        }
        else
        {
            WriteArrayEndToken();
        }
    }

    public override void Set(string name, ref int value, int defaultValue = 0, int firstVersion = -1, int lastVersion = -1)
    {
        bool validVersion = CheckVersion(ref value, defaultValue, firstVersion, ref lastVersion);

        if (!validVersion)
            return;

        bool validClassMember = CheckClassBitmask(ref value, defaultValue);
        if (!validClassMember)
            return;

        if (isReading)
        {
            value = ReadInt();
            return;
        }
        else
        {
            writer.Write(value);
            DebugMsg(string.Format("Write int: {0}", value));
        }
    }

    public override void Set(string name, ref float value, float defaultValue = 0, int firstVersion = -1, int lastVersion = -1)
    {
        bool validVersion = CheckVersion(ref value, defaultValue, firstVersion, ref lastVersion);

        if (!validVersion)
            return;

        bool validClassMember = CheckClassBitmask(ref value, defaultValue);
        if (!validClassMember)
            return;

        if (isReading)
        {
            value = ReadFloat();
            return;
        }
        else
        {
            writer.Write(value);
            DebugMsg(string.Format("Write float: {0}", value));
        }
    }

    public override void Set(string name, ref Vector3 value, Vector3 defaultValue, int firstVersion = -1, int lastVersion = -1)
    {
        bool validVersion = CheckVersion(ref value, defaultValue, firstVersion, ref lastVersion);

        if (!validVersion)
            return;

        bool validClassMember = CheckClassBitmask(ref value, defaultValue);
        if (!validClassMember)
            return;

        if (isReading)
        {
            float x = ReadFloat();
            float y = ReadFloat();
            float z = ReadFloat();

            value = new Vector3(x, y, z);
            return;
        }
        else
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);

            DebugMsg(string.Format("Write vector3: {0} {1} {2}", value.x, value.y, value.z));
        }
    }

    private void ThrowException(bool invalid, string message)
    {
        if (invalid)
            throw new IOException(message);
    }

    private void ThrowAssert(bool invalid, string message)
    {
        if (invalid)
            Debug.Assert(!invalid, message);
    }
}