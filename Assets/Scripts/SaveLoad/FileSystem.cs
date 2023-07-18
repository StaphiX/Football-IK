
using System;
using System.IO;
using UnityEngine;

public static class FileSystem
{
    public static void SaveFile(string path, string fileName, byte[] fileBytes)
    {
        CreateDirRecursive(path);

        string pathDir = path + Path.DirectorySeparatorChar;
        string fullPath = pathDir + fileName;

        try
        {
            File.WriteAllBytes(fullPath, fileBytes);
            Debug.LogFormat("{0} saved to: {1}", fileName, fullPath);
        }
        catch (Exception e)
        {
            Debug.LogFormat("Failed to save data to: {0}", fullPath);
            Debug.LogError("Error " + e.Message);
        }
    }

    public static void SaveFile(string path, string fileName, string allText)
    {
        CreateDirRecursive(path);

        string pathDir = path + Path.DirectorySeparatorChar;
        string fullPath = pathDir + fileName;

        try
        {
            File.WriteAllText(fullPath, allText);
            Debug.LogFormat("{0} saved to: {1}", fileName, fullPath);
        }
        catch (Exception e)
        {
            Debug.LogFormat("Failed to save data to: {0}", fullPath);
            Debug.LogError("Error " + e.Message);
        }
    }

    public static bool LoadFile(string path, string fileName, out byte[] fileBytes)
    {
        string pathDir = path + Path.DirectorySeparatorChar;
        if (!Directory.Exists(Path.GetDirectoryName(pathDir)))
        {
            Debug.LogFormat("Can not find directory: {0}", path);
            fileBytes = null;
            return false;
        }

        string fullPath = pathDir + fileName;

        if (!File.Exists(fullPath))
        {
            Debug.LogFormat("File does not exist: {0}", fullPath);
            fileBytes = null;
            return false;
        }

        try
        {
            fileBytes = File.ReadAllBytes(fullPath);
            Debug.LogFormat("Loaded file: {0}", fullPath);

            return true;
        }
        catch (Exception e)
        {
            Debug.LogFormat("Failed to load data from: {0}", fullPath);
            Debug.LogError("Error " + e.Message);

            fileBytes = null;
            return false;
        }
    }

    public static bool LoadFile(string path, string fileName, out string allText)
    {
        string pathDir = path + Path.DirectorySeparatorChar;
        if (!Directory.Exists(Path.GetDirectoryName(pathDir)))
        {
            Debug.LogFormat("Can not find directory: {0}", path);
            allText = null;
            return false;
        }

        string fullPath = pathDir + fileName;

        if (!File.Exists(fullPath))
        {
            Debug.LogFormat("File does not exist: {0}", fullPath);
            allText = null;
            return false;
        }

        try
        {
            allText = File.ReadAllText(fullPath);
            Debug.LogFormat("Loaded file: {0}", fullPath);

            return true;
        }
        catch (Exception e)
        {
            Debug.LogFormat("Failed to load data from: {0}", fullPath);
            Debug.LogError("Error " + e.Message);

            allText = null;
            return false;
        }
    }

    public static bool DeleteFile(string path, string fileName)
    {
        string pathDir = path + Path.DirectorySeparatorChar;
        if (!Directory.Exists(Path.GetDirectoryName(pathDir)))
        {
            Debug.LogFormat("Can not find directory: {0}", path);
            return false;
        }

        string fullPath = pathDir + fileName;

        if (!File.Exists(fullPath))
        {
            Debug.LogFormat("File does not exist: {0}", fullPath);
            return false;
        }

        try
        {
            File.Delete(fullPath);
            Debug.LogFormat("Deleted file: {0}", fullPath);

            return true;
        }
        catch (Exception e)
        {
            Debug.LogFormat("Failed to delete file from: {0}", fullPath);
            Debug.LogError("Error " + e.Message);
            return false;
        }
    }

    public static void CreateDirRecursive(string dirPath)
    {
        string pathDir = dirPath + Path.DirectorySeparatorChar;
        string dirName = Path.GetDirectoryName(pathDir);
        if(dirName.Length > 0 && !Directory.Exists(dirName))
        {
            CreateDirRecursive(dirName);
            Directory.CreateDirectory(dirName);
        }
    }

    public static bool Exists(string filePath)
    {
        string pathDir = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(pathDir))
        {
            Debug.LogFormat("Can not find directory: {0}", pathDir);
            return false;
        }

        bool fileExists = File.Exists(filePath);

        Debug.AssertFormat(fileExists, "File not found: {0}", filePath);

        return fileExists;

    }
}