using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static AtlasSpawn;

[CreateAssetMenu(fileName = "Trip", menuName = "Atlas / Trip")]
public class TripSO : ScriptableObject
{
    public ZoneSpawnerData[] zoneSpawnerData;
    public StationSO[] stationsDataArray;
    public NPCBrain[] npc_prefabsArray;


    [Header("Settings")]
    public int minStationsTraitorsTravel = 2;
    public int maxStationsTraitorsTravel = 4;
    [Header("Generated")]
    public StationSO curStation;
    
    public int totalTicketsToCheck;

    public int ticketsCheckedSinceStart;
    public int ticketsCheckedSinceLastStation;
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ZoneSpawnerData))]
public class ZoneSpawnerDataDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty areaProp = property.FindPropertyRelative("area");

        if (areaProp != null)
        {
            label.text = areaProp.enumDisplayNames[areaProp.enumValueIndex];
        }

        EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}
#endif
