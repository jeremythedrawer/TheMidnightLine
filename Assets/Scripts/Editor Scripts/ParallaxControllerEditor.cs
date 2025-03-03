using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ParallaxController))]
public class ParallaxControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ParallaxController parallaxController = (ParallaxController)target;

        DrawDefaultInspector();

        if (parallaxController.GetComponentInParent<Spawner>() is OneShotSpawner && parallaxController.GetComponent<SkyLine>() == null)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("spawnPosition"), new GUIContent("Spawn Position"));
        }

        serializedObject.ApplyModifiedProperties();
    }
}