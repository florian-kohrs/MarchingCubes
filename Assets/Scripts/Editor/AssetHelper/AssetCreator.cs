using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


public class AssetCreator
{

    public const string ASSET_PATH = "Assets";
    public const string ASSET_FILE_EXTENSION = ".asset";
    public const char DIRECTORY_SEPERATOR = '/';

    /// <summary>
    /// creates an asset of type t at given path
    /// </summary>
    /// <typeparam name="T">type of asset</typeparam>
    /// <param name="path">relative path to safe assets</param>
    /// <param name="selectAsset">will focus the asset in inspector</param>
    public static void CreateAsset<T>(string path, bool selectAsset = true) where T : ScriptableObject
    {
        string localPath = CreatePath(path);
        ScriptableObject asset = CreateAsset<T>(localPath);

        if (selectAsset)
        {
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }

    /// <summary>
    /// creates the directories to given path
    /// </summary>
    /// <param name="path"></param>
    /// <returns>returns the relative path starting at assets</returns>
    private static string CreatePath(string path)
    {
        string currentPath = ASSET_PATH;
        foreach (string s in path.Split(DIRECTORY_SEPERATOR))
        {
            string nextPath = Path.Combine(currentPath, s);
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, s);
            }
            currentPath = nextPath;
        }
        return currentPath;
    }


    private static ScriptableObject CreateAsset<T>(string path) where T : ScriptableObject
    {
        ScriptableObject asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, Path.Combine(path, System.Guid.NewGuid().ToString()) + ASSET_FILE_EXTENSION);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        return asset;
    }
}
