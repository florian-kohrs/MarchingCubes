using System.Collections;
using System.Collections.Generic;
using MarchingCubes;
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


    [MenuItem("Assets/Create/Custom/MarchingCubes/Biom")]
    public static void NewBiom()
    {
        AssetCreator.CreateAsset<BiomScriptableObject>(BASE_FOLDER_NAME + "Biom");
    }

}
