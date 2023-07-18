using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

public class JsonSerializer : FileSerializer, IDisposable
{
    private StringBuilder sb = null;
    private StringWriter sw = null;
    private StringReader sr = null;
    private JsonWriter writer = null;
    private JsonReader reader = null;
    private bool isClosed = false;

    public JsonSerializer(uint latestVersion, string file) : base(latestVersion, 0, true)
    {
        sr = new StringReader(file);
        reader = new JsonTextReader(sr);

        ReadObj("");
        serializeVersion = (uint)ReadInt("version");
    }

    public JsonSerializer(uint latestVersion) : base(latestVersion, latestVersion, false)
    {
        sb = new StringBuilder();
        sw = new StringWriter(sb);
        writer = new JsonTextWriter(sw);
        //writer.Formatting = Formatting.Indented;
        writer.WriteStartObject();
        writer.WritePropertyName("version");
        writer.WriteValue(latestVersion);
    }

    ~JsonSerializer()
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

        writer.WriteEndObject();
        writer.Close();
        sw.Flush();
    }

    public string Close()
    {
        Finish();

        if (isReading)
        {
            return null;
        }
        else
        {
            return writer == null ? null : sb.ToString();
        }
    }

    public override bool CheckVersion<T>(ref T value, T defaultValue, int firstVersion, ref int lastVersion)
    {
        bool validVersion = base.CheckVersion<T>(ref value, defaultValue, firstVersion, ref lastVersion);
        if (isReading && IsObsolete(lastVersion))
        {
            reader.Skip();
        }

        return validVersion;
    }

    private void ReadObj(string name)
    {
        string objectName = "";
        reader.Read();
        if (reader.TokenType == JsonToken.PropertyName)
        {
            objectName = (string)reader.Value;
            JsonAssert(objectName != name, string.Format("Incorrect Object Name: {0}", name));
            reader.Read();
        }
        JsonException(reader.TokenType != JsonToken.StartObject && reader.TokenType != JsonToken.EndObject,
            string.Format("Failed to find object token: {0}\"", reader.TokenType));
    }

    private void ReadArray(string name)
    {
        string arrayName = "";
        reader.Read();
        if (reader.TokenType == JsonToken.PropertyName)
        {
            arrayName = (string)reader.Value;
            JsonAssert(arrayName != name, string.Format("Incorrect Array Name: {0}", name));
            reader.Read();
        }
        JsonException(reader.TokenType != JsonToken.StartArray && reader.TokenType != JsonToken.EndArray,
            string.Format("Failed to find array token: {0}\"", reader.TokenType));
    }

    private int ReadInt(string name)
    {
        string propName = "";
        bool success = reader.Read();
        if (reader.TokenType == JsonToken.PropertyName)
        {
            propName = (string)reader.Value;
            JsonAssert(propName != name, string.Format("Incorrect Property Name: {0}", name));
            success = reader.Read();
        }
        JsonException(reader.TokenType != JsonToken.Integer, string.Format("Failed to find int token: {0}", reader.TokenType));

        return success ? Convert.ToInt32(reader.Value) : 0;
    }

    private string ReadString(string name)
    {
        string propName = "";
        bool success = reader.Read();
        if (reader.TokenType == JsonToken.PropertyName)
        {
            propName = (string)reader.Value;
            JsonAssert(propName != name, string.Format("Incorrect Property Name: {0}", name));
            success = reader.Read();
        }
        JsonException(reader.TokenType != JsonToken.String, string.Format("Failed to find string token: {0}", reader.TokenType));
        return success ? (string)reader.Value : null;
    }

    private float ReadFloat(string name)
    {
        string propName = "";
        bool success = reader.Read();
        if (reader.TokenType == JsonToken.PropertyName)
        {
            propName = (string)reader.Value;
            JsonAssert(propName != name, string.Format("Incorrect Property Name: {0}", name));
            success = reader.Read();
        }
        JsonException(reader.TokenType != JsonToken.Float, string.Format("Failed to find float token: {0}", reader.TokenType));
        return success ? (float)Convert.ToDecimal(reader.Value) : 0;
    }

    public override void SetObject<T>(string name, ref T obj, CreateNew<T> createNew, GetType<T> getType, int firstVersion, int lastVersion)
    {
        bool validVersion = CheckVersion(ref obj, default, firstVersion, ref lastVersion);

        if (!validVersion)
            return;

        if (!isReading)
            writer.WriteStartObject();
        else
        {
            // Read Obj
            reader.Read();

            //If we have a start object token then we can continue
            JsonException(reader.TokenType != JsonToken.StartObject, string.Format("Failed to find start object {0}", reader.TokenType));
        }

        int objectType = 0;
        if (isReading)
        {
            if (getType != null)
                objectType = ReadInt("type");

            obj = (createNew(objectType));
        }
        else if (getType != null)
        {
            objectType = getType(obj);
            writer.WritePropertyName("type");
            writer.WriteValue(objectType);
        }

        obj.Serialize(this);

        if (!isReading)
            writer.WriteEndObject();
        else
        {
            ReadObj("");
        }
    }

    public override void SetObjectArray<T>(string name, ref List<T> list, CreateNew<T> createNew, GetType<T> getType, int firstVersion, int lastVersion)
    {
        bool validVersion = CheckVersion(ref list, null, firstVersion, ref lastVersion);

        if (isReading && list == null)
            list = new List<T>();

        if (!validVersion)
            return;

        int listCount = list.Count;
        if (isReading)
        {
            ReadArray(name);
        }
        else
        {
            if (name != null && name.Length > 0)
                writer.WritePropertyName(name);
            writer.WriteStartArray();
        }

        int index = 0;
        while (index < listCount || isReading)
        {
            if (!isReading)
                writer.WriteStartObject();
            else
            {
                // Read Obj
                reader.Read();

                // Have we run out of objects
                if (reader.TokenType == JsonToken.EndArray)
                    break;

                //If we have a start object token then we can continue
                JsonException(reader.TokenType != JsonToken.StartObject, string.Format("Failed to find start object {0}", reader.TokenType));
            }

            int objectType = 0;
            if (isReading)
            {
                if (getType != null)
                    objectType = ReadInt("type");

                list.Add(createNew(objectType));
            }
            else if (getType != null)
            {
                objectType = getType(list[index]);
                writer.WritePropertyName("type");
                writer.WriteValue(objectType);
            }

            list[index].Serialize(this);

            if (!isReading)
                writer.WriteEndObject();
            else
            {
                ReadObj("");
            }

            ++index;
        }

        if (isReading)
        {
            // Closing array tag is read in the ReadObj call above in while loop
            JsonException(reader.TokenType != JsonToken.EndArray, string.Format("Failed to find end of array: {0}", reader.TokenType));
        }
        else
        {
            writer.WriteEndArray();
        }
    }

    public override void Set(string name, ref int value, int defaultValue, int firstVersion, int lastVersion)
    {
        bool validVersion = CheckVersion(ref value, defaultValue, firstVersion, ref lastVersion);

        if (!validVersion)
            return;

        if (isReading)
        {
            value = ReadInt(name);
            return;
        }
        else
        {
            if (name != null && name.Length > 0)
                writer.WritePropertyName(name);
            writer.WriteValue(value);
            return;
        }
    }

    public override void Set(string name, ref float value, float defaultValue, int firstVersion, int lastVersion)
    {
        bool validVersion = CheckVersion(ref value, defaultValue, firstVersion, ref lastVersion);

        if (!validVersion)
            return;

        if (isReading)
        {
            value = ReadFloat(name);
            return;
        }
        else
        {
            if (name != null && name.Length > 0)
                writer.WritePropertyName(name);
            writer.WriteValue(value);
            return;
        }
    }

    public override void Set(string name, ref Vector3 value, Vector3 defaultValue, int firstVersion, int lastVersion)
    {
        bool validVersion = CheckVersion(ref value, defaultValue, firstVersion, ref lastVersion);

        if (!validVersion)
            return;

        if (isReading)
        {
            ReadArray(name);
            float x = ReadFloat("");
            float y = ReadFloat("");
            float z = ReadFloat("");
            value = new Vector3(x, y, z);
            ReadArray("");
            return;
        }
        else
        {
            if (name != null && name.Length > 0)
                writer.WritePropertyName(name);
            writer.WriteStartArray();
            writer.WriteValue(value.x);
            writer.WriteValue(value.y);
            writer.WriteValue(value.z);
            writer.WriteEndArray();
            return;
        }
    }

    private void JsonException(bool invalid, string message)
    {
        if(invalid)
            throw new JsonReaderException(message);
    }

    private void JsonAssert(bool invalid, string message)
    {
        if (invalid)
            Debug.Assert(!invalid, message);
    }
}