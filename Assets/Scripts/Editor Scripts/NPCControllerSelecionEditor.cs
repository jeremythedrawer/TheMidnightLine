using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NPCCore), true)]
public class NPCControllerSelecionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        NPCCore npcCore = (NPCCore)target;

        SerializedObject serializedObject = new SerializedObject(npcCore);
        SerializedProperty boxColliderProperty = serializedObject.FindProperty("boxCollider2D");

        //agents
        SerializedProperty agentControllerProperty = serializedObject.FindProperty("agentController");

        //bystanders
        SerializedProperty bystanderControllerProperty = serializedObject.FindProperty("bystanderController");

        DrawDefaultInspector();

        if (!serializedObject.isEditingMultipleObjects && boxColliderProperty.objectReferenceValue != null)
        {
            GameObject colliderGameObject = ((BoxCollider2D)boxColliderProperty.objectReferenceValue).gameObject;

            if (colliderGameObject.CompareTag("Agent Collider"))
            {
                EditorGUILayout.PropertyField(agentControllerProperty, new GUIContent("Agent Controller"));
            }
            else if (colliderGameObject.CompareTag("Bystander Collider"))
            {
                EditorGUILayout.PropertyField(bystanderControllerProperty, new GUIContent("Bystander Controller"));
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}

