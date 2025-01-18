using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(StateList))]
public class StateSelectionEditor : Editor
{
    public override void OnInspectorGUI()
    {

        StateList stateList = (StateList)target;

        SerializedObject serializedObject = new SerializedObject(stateList);

        DrawDefaultInspector();

        // Conditionally display the state fields
        if (stateList.airborne) EditorGUILayout.PropertyField(serializedObject.FindProperty("airborneState"), new GUIContent("Airborne State"));
        if (stateList.jumped) EditorGUILayout.PropertyField(serializedObject.FindProperty("jumpedState"), new GUIContent("Jumped State"));
        if (stateList.fall) EditorGUILayout.PropertyField(serializedObject.FindProperty("fallState"), new GUIContent("Fall State"));
        if (stateList.airMelee) EditorGUILayout.PropertyField(serializedObject.FindProperty("airMeleeState"), new GUIContent("Air Melee State"));
        if (stateList.airShoot) EditorGUILayout.PropertyField(serializedObject.FindProperty("airShootState"), new GUIContent("Air Shoot State"));

        if (stateList.ground) EditorGUILayout.PropertyField(serializedObject.FindProperty("groundState"), new GUIContent("Ground State"));
        if (stateList.idle) EditorGUILayout.PropertyField(serializedObject.FindProperty("idleState"), new GUIContent("Idle State"));
        if (stateList.run) EditorGUILayout.PropertyField(serializedObject.FindProperty("runState"), new GUIContent("Run State"));
        if (stateList.melee) EditorGUILayout.PropertyField(serializedObject.FindProperty("meleeState"), new GUIContent("Melee State"));
        if (stateList.shoot) EditorGUILayout.PropertyField(serializedObject.FindProperty("shootState"), new GUIContent("Shoot State"));

        if (stateList.wall) EditorGUILayout.PropertyField(serializedObject.FindProperty("wallState"), new GUIContent("Wall State"));
        if (stateList.hang) EditorGUILayout.PropertyField(serializedObject.FindProperty("hangState"), new GUIContent("Hang State"));
        if (stateList.climb) EditorGUILayout.PropertyField(serializedObject.FindProperty("climbState"), new GUIContent("Climb State"));

        if (stateList.attack) EditorGUILayout.PropertyField(serializedObject.FindProperty("attackState"), new GUIContent("Attack State"));
        if (stateList.hiding) EditorGUILayout.PropertyField(serializedObject.FindProperty("hidingState"), new GUIContent("Hiding State"));
        if (stateList.stalk) EditorGUILayout.PropertyField(serializedObject.FindProperty("stalkState"), new GUIContent("Stalk State"));

        if (stateList.calm) EditorGUILayout.PropertyField(serializedObject.FindProperty("calmState"), new GUIContent("Calm State"));
        if (stateList.flee) EditorGUILayout.PropertyField(serializedObject.FindProperty("fleeState"), new GUIContent("Flee State"));
        if (stateList.leave) EditorGUILayout.PropertyField(serializedObject.FindProperty("leaveState"), new GUIContent("Leave State"));
        if (stateList.panic) EditorGUILayout.PropertyField(serializedObject.FindProperty("panicState"), new GUIContent("Panic State"));

        if (stateList.sacrifice) EditorGUILayout.PropertyField(serializedObject.FindProperty("sacrificeState"), new GUIContent("Sacrifice State"));
        if (stateList.checkTicket) EditorGUILayout.PropertyField(serializedObject.FindProperty("checkTicketState"), new GUIContent("Check Ticket State"));

        serializedObject.ApplyModifiedProperties();
    }
}

