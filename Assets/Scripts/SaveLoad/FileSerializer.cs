using System;
using System.Collections.Generic;
using UnityEngine;

public interface ISerialize
{
    public void Serialize(FileSerializer serializer);
}

public abstract class FileSerializer
{
    public FileSerializer(uint latestVersion, uint serializeVersion, bool isReading) { this.latestVersion = latestVersion; this.serializeVersion = serializeVersion; this.isReading = isReading; }
    public uint latestVersion;
    public uint serializeVersion;
    public bool isReading = false;
    public bool isDebugging = false;
    public bool IsOldVersion(int firstVersion) { return serializeVersion < firstVersion; }
    public bool IsObsolete(int lastVersion) { return lastVersion >= 0 && serializeVersion > lastVersion; }
    public abstract void Set(string name, ref int value, int defaultValue = 0, int firstVersion = -1, int lastVersion = -1);
    public abstract void Set(string name, ref float value, float defaultValue = 0, int firstVersion = -1, int lastVersion = -1);
    public abstract void Set(string name, ref Vector3 value, Vector3 defaultValue, int firstVersion = -1, int lastVersion = -1);

    public delegate T CreateNew<T>(int typeValue);
    public delegate int GetType<T>(T objectOftype);

    public abstract void SetObject<T>(string name, ref T obj, CreateNew<T> createNew, GetType<T> getType = null, int firstVersion = -1, int lastVersion = -1) where T : ISerialize;
    public abstract void SetObjectArray<T>(string name, ref List<T> list, CreateNew<T> createNew, GetType<T> getType = null, int firstVersion = -1, int lastVersion = -1) where T : ISerialize;

    public virtual bool CheckVersion<T>(ref T value, T defaultValue, int firstVersion, ref int lastVersion)
    {
        lastVersion = Mathf.Max(firstVersion, lastVersion);

        if(firstVersion > latestVersion && lastVersion > latestVersion)
            throw new ArgumentException("Need to increment version in Versions.cs to fix serializer");

        if (isReading && IsOldVersion(firstVersion) || IsObsolete(lastVersion))
        {
            value = defaultValue;
            return false;
        }

        if (!isReading && IsOldVersion(firstVersion) || IsObsolete(lastVersion))
        {
            return false;
        }

        return true;
    }

    // Debug Helpers
    public virtual void Description(string description)
    {
        DebugMsg(description);
    }

    protected virtual void DebugMsg(string message)
    {
        if (!isDebugging)
            return;

        Debug.Log(message);
    }
}
