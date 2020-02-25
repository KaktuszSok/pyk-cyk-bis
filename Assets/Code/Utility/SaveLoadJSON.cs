using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public static class SaveLoadJSON {


    /// <param name="directory">path relative to Application.persistentDataPath/saved/ e.g. "crafts"</param>
    /// <param name="fileName">name of file, remember to include file extension. e.g. "craftname.bp"</param>
    public static void SaveObjToFile(object obj, string directory, string fileName)
    {
        string json = JsonUtility.ToJson(obj);
        string dirPath = GetFullPath(directory);
        string filePath = dirPath + "/" + fileName;

        //Make sure directory exists
        if(!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }

        File.WriteAllText(filePath, json);
        Debug.Log("Saved JSON file at path " + filePath);
    }


    /// <param name="pathIsRelative">is the path relative to Application.persistentDataPath/saved/</param>
    public static T LoadObjFromFile<T>(string filePath, bool pathIsRelative = false)
    {
        if (pathIsRelative) filePath = GetFullPath(filePath);
        if (!File.Exists(filePath))
        {
            Debug.LogWarning("Unsuccessful in loading object of type " + typeof(T).ToString() + " from file " + filePath);
            return default(T);
        }
        return JsonUtility.FromJson<T>(File.ReadAllText(filePath));
    }

    public static string[] SearchForFiles(string directory, string searchPattern, bool returnFullPaths = false)
    {
        if (!Directory.Exists(GetFullPath(directory))) return new string[0]; //Directory couldn't be found - return empty list
        if(returnFullPaths) return Directory.GetFiles(GetFullPath(directory), searchPattern);
        else
        {
            string[] fullPaths = Directory.GetFiles(GetFullPath(directory), searchPattern);
            string[] fileNames = new string[fullPaths.Length];
            for(int i = 0; i < fullPaths.Length; i++)
            {
                fileNames[i] = Path.GetFileNameWithoutExtension(fullPaths[i]);
            }
            return fileNames;
        }
    }

    /// <param name="path">path relative to Application.persistentDataPath/saved/</param>
    public static string GetFullPath(string path)
    {
        return Application.persistentDataPath + "/saved/" + path;
    }

}
