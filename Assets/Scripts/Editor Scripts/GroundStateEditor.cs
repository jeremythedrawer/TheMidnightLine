using UnityEngine;
using UnityEditor;
using UnityEditor.TerrainTools;

[CustomEditor(typeof(GroundState))]
public class GroundStateEditor : Editor
{
    public override void OnInspectorGUI()
    {
        GroundState groundState = (GroundState)target;

        DrawDefaultInspector();

        if (groundState.canMelee)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("meleeState"), new GUIContent("Melee State"));
        }
        if (groundState.canShoot)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("shootState"), new GUIContent("Shoot State"));
        }

        serializedObject.ApplyModifiedProperties();
    }
}
