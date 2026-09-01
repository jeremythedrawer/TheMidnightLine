using System;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using static Passenger;

[CreateAssetMenu(fileName = "NPCs_Data_SO", menuName = "Midnight Line SOs / NPCs Data SO")]
public class PassengerData : ScriptableObject
{
    public UnityEngine.Object habitsFolder; 
    public Graffiti graffitiPrefab;
    
    [Header("Generated")]
    public HabitData[] habitDataArray;
    public Dictionary<Habits, string> habitStringDict;
    public Dictionary<Habits, HabitData> habitDataDict;

#if UNITY_EDITOR
    public void OnValidate()
    {
        UpdateHabitDataArray();
    }
    public void UpdateHabitDataArray()
    {
        if (habitsFolder == null) return;

        string folderPath = AssetDatabase.GetAssetPath(habitsFolder);

        List<HabitData> habitList = new List<HabitData>();

        string[] folderPathArray = new string[] { folderPath };
        string[] habitGuids = AssetDatabase.FindAssets("t:" + nameof(HabitData), folderPathArray);

        for (int i = 0; i < habitGuids.Length; i++)
        {
            string guid = habitGuids[i];
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            HabitData passengerHabitData = AssetDatabase.LoadAssetAtPath<HabitData>(assetPath);

            if (passengerHabitData != null)
            {
                habitList.Add(passengerHabitData);
            }
        }

        habitDataArray = habitList.ToArray();
        EditorUtility.SetDirty(this);
    }
#endif
}
#if UNITY_EDITOR
public class HabitFolderObserver : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        string[] guids = AssetDatabase.FindAssets("t:" + nameof(PassengerData));

        for (int i = 0; i < guids.Length; i++)
        {
            string guid = guids[i];
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            PassengerData passengerData = AssetDatabase.LoadAssetAtPath<PassengerData>(assetPath);
            if (passengerData == null || passengerData.habitsFolder == null) continue;

            string habitsFolderPath = AssetDatabase.GetAssetPath(passengerData.habitsFolder);

            if (string.IsNullOrEmpty(habitsFolderPath)) continue;

            bool importAssetsChanged = AssetsAffectFolder(importedAssets, habitsFolderPath);
            bool deletedAssetsChanged = AssetsAffectFolder(deletedAssets, habitsFolderPath);
            bool movedAssetsChanged = AssetsAffectFolder(movedAssets, habitsFolderPath);
            bool movedFromAssetPathsChanged = AssetsAffectFolder(movedFromAssetPaths, habitsFolderPath);

            if (importAssetsChanged || deletedAssetsChanged || movedAssetsChanged || movedFromAssetPathsChanged)
            {
                passengerData.UpdateHabitDataArray();
            }
        }
    }
    static bool AssetsAffectFolder(string[] assetPaths, string folderPath)
    {
        for (int i = 0; i < assetPaths.Length; i++)
        {
            string assetPath = assetPaths[i];

            if (assetPath == folderPath) return true;
            if (assetPath.StartsWith(folderPath + "/")) return true;
        }
        return false;
    }
}
#endif