using UnityEditor;
using UnityEngine;
using static AtlasSpawn;

[CreateAssetMenu(fileName = "Trip", menuName = "Atlas / Trip")]
public class TripSO : ScriptableObject
{
    public ZoneSpawnerData[] zoneSpawnerData;
    public StationSO[] stations;

    [Header("Generated")]
    public int curStationIndex;
    public StationSO curStation;
    public Zone[][] zonesByArea;
    public int tripMeters;
}

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
