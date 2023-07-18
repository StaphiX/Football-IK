using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEditor.AssetImporters;
using UnityEngine;

public static class BipedAnimSerializer
{
    public static string SaveLocation = Application.persistentDataPath + Path.DirectorySeparatorChar + "PoseAnimations";
    public static string ResourcesJsonLocation = "PoseAnimations/Json";
    public static string ResourcesDataLocation = "PoseAnimations/Data";

    public static void SaveBinary(string animName, PoseAnimation animation)
    {
        if (animation.keyframes == null || animation.keyframes.Count < 1)
            return;

        string filePath = SaveLocation + Path.DirectorySeparatorChar + animName + ".dat";

        try
        {
            using (BinarySerializer binarySerializer = new BinarySerializer(Versions.BipedAnimVersion, filePath, false))
            {
                if (binarySerializer.isInitialised)
                    animation.Serialize(binarySerializer);
            }
        }
        catch (Exception e)
        {
            Exception baseEx = e.GetBaseException();
            Debug.Assert(false, baseEx.Message);
            Debug.Assert(false, baseEx.StackTrace);
        }
    }

    public static void SaveJson(string animName, PoseAnimation animation)
    {
        if (animation.keyframes == null || animation.keyframes.Count < 1)
            return;

        string filePath = SaveLocation + Path.DirectorySeparatorChar + animName + ".json";

        try
        {
            using (JsonSerializer jsonSerializer = new JsonSerializer(Versions.BipedAnimVersion))
            {
                animation.Serialize(jsonSerializer);

                string json = jsonSerializer.Close();
                FileSystem.SaveFile(SaveLocation, animName + ".json", json);
            }
        }
        catch (Exception e)
        {
            Exception baseEx = e.GetBaseException();
            Debug.Assert(false, baseEx.Message);
            Debug.Assert(false, baseEx.StackTrace);
        }
    }

    public static bool LoadBinary(string animName, out PoseAnimation animation)
    {
        TextAsset resourcesFile = Resources.Load<TextAsset>(ResourcesDataLocation + animName);

        animation = null;

        if (resourcesFile)
        {
            animation = new PoseAnimation();

            try
            {
                using (BinarySerializer binarySerializer = new BinarySerializer(Versions.BipedAnimVersion, resourcesFile.bytes))
                {
                    if (binarySerializer.isInitialised)
                    {
                        animation = new PoseAnimation();
                        animation.Serialize(binarySerializer);
                    }
                }
            }
            catch (Exception e)
            {
                Exception baseEx = e.GetBaseException();
                Debug.Assert(false, baseEx.Message);
                Debug.Assert(false, baseEx.StackTrace);
                return false;
            }

            return true;
        }

        string filePath = SaveLocation + Path.DirectorySeparatorChar + animName + ".dat";

        try
        {
            using (BinarySerializer binarySerializer = new BinarySerializer(Versions.BipedAnimVersion, filePath, true))
            {
                if (binarySerializer.isInitialised)
                {
                    animation = new PoseAnimation();
                    animation.Serialize(binarySerializer);
                }
            }
        }
        catch (Exception e)
        {
            Exception baseEx = e.GetBaseException();
            Debug.Assert(false, baseEx.Message);
            Debug.Assert(false, baseEx.StackTrace);
            return false;
        }

        return true;
    }

    public static bool LoadJson(string animName, out PoseAnimation animation)
    {
        TextAsset resourcesFile = Resources.Load<TextAsset>(ResourcesJsonLocation + animName);

        animation = null;

        if (resourcesFile)
        {
            animation = new PoseAnimation();

            try
            {
                using (JsonSerializer jsonSerializer = new JsonSerializer(Versions.BipedAnimVersion, resourcesFile.text))
                {
                    animation = new PoseAnimation();
                    animation.Serialize(jsonSerializer);
                }
            }
            catch (Exception e)
            {
                Exception baseEx = e.GetBaseException();
                Debug.Assert(false, baseEx.Message);
                Debug.Assert(false, baseEx.StackTrace);
                return false;
            }

            return true;
        }

        try
        {
            bool success = FileSystem.LoadFile(SaveLocation, animName + ".json", out string allText);
            if (!success)
                return false;
            using (JsonSerializer binarySerializer = new JsonSerializer(Versions.BipedAnimVersion, allText))
            {
                animation = new PoseAnimation();
                animation.Serialize(binarySerializer);
            }
        }
        catch (Exception e)
        {
            Exception baseEx = e.GetBaseException();
            Debug.Assert(false, baseEx.Message);
            Debug.Assert(false, baseEx.StackTrace);
            return false;
        }

        return true;
    }
}

public class ByteAsset : TextAsset
{
    public ByteAsset(byte[] data) : base("") { rawBytes = data; }
    public byte[] rawBytes;
}