using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AssetMenue : MonoBehaviour
{

    private const string BASE_FOLDER_NAME = "ScriptableObjects/";

    [MenuItem("Assets/Create/Custom/BuildingBlock")]
    public static void NewMovement()
    {
        AssetCreator.CreateAsset<BaseBuildingBlock>(BASE_FOLDER_NAME + "Building");
    }

    
}
