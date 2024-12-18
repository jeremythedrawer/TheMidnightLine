using UnityEngine;
using UnityEditor;

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
