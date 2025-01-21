using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;

[CustomEditor(typeof(StateList))]
public class StateSelectionEditor : Editor
{
    public override void OnInspectorGUI()
    {

        StateList stateList = (StateList)target;

        SerializedObject serializedObject = new SerializedObject(stateList);

        DrawDefaultInspector();

        EditorGUILayout.LabelField("Character States", EditorStyles.boldLabel);

        switch (stateList.characterType)
        {
            case StateList.CharacterType.Player:
                DrawPlayerStates(stateList, serializedObject);
                break;
            case StateList.CharacterType.Agent:
                DrawAgentStates(stateList, serializedObject);
                break;
            case StateList.CharacterType.Bystander:
                DrawBystanderStates(stateList, serializedObject);
                break;

        }

        serializedObject.ApplyModifiedProperties();
    }
    private void DrawPlayerStates(StateList stateList, SerializedObject serializedObject)
    {
        EditorGUILayout.LabelField("Environmental States", EditorStyles.miniBoldLabel);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("groundState"), new GUIContent("Ground State"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("airborneState"), new GUIContent("Airborne State"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("wallState"), new GUIContent("Wall State"));

        EditorGUILayout.LabelField("Action States", EditorStyles.miniBoldLabel);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("idleState"), new GUIContent("Idle State"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("runState"), new GUIContent("Run State"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("meleeState"), new GUIContent("Melee State"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("shootState"), new GUIContent("Shoot State"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("jumpedState"), new GUIContent("Jumped State"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("fallState"), new GUIContent("Fall State"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("airMeleeState"), new GUIContent("Air Melee State"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("airShootState"), new GUIContent("Air Shoot State"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("hangState"), new GUIContent("Hang State"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("climbState"), new GUIContent("Climb State"));
    }

    private void DrawAgentStates(StateList stateList, SerializedObject serializedObject)
    {
        EditorGUILayout.LabelField("Behavioural States", EditorStyles.miniBoldLabel);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("calmState"), new GUIContent("Calm State"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("stalkState"), new GUIContent("Stalk State"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("attackState"), new GUIContent("Attack State"));

        EditorGUILayout.LabelField("Environmental States", EditorStyles.miniBoldLabel);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("groundState"), new GUIContent("Ground State"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("airborneState"), new GUIContent("Airborne State"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("wallState"), new GUIContent("Wall State"));

        EditorGUILayout.LabelField("Action States", EditorStyles.miniBoldLabel);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("idleState"), new GUIContent("Idle State"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("runState"), new GUIContent("Run State"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("meleeState"), new GUIContent("Melee State"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("shootState"), new GUIContent("Shoot State"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("jumpedState"), new GUIContent("Jumped State"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("fallState"), new GUIContent("Fall State"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("airMeleeState"), new GUIContent("Air Melee State"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("airShootState"), new GUIContent("Air Shoot State"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("hangState"), new GUIContent("Hang State"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("climbState"), new GUIContent("Climb State"));

    }

    private void DrawBystanderStates(StateList stateList, SerializedObject serializedObject)
    {
        EditorGUILayout.LabelField("Behavioural States", EditorStyles.miniBoldLabel);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("calmState"), new GUIContent("Calm State"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("panicState"), new GUIContent("Panic State"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("leaveState"), new GUIContent("Leave State"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("fleeState"), new GUIContent("Flee State"));

        EditorGUILayout.LabelField("Environmental States", EditorStyles.miniBoldLabel);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("groundState"), new GUIContent("Ground State"));

        EditorGUILayout.LabelField("Action States", EditorStyles.miniBoldLabel);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("idleState"), new GUIContent("Idle State"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("runState"), new GUIContent("Run State"));

    }
}


