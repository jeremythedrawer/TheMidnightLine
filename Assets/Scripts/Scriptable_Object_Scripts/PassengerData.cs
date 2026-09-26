using UnityEngine;
using static Passenger;
using static Atlas;


#if UNITY_EDITOR
using UnityEditor;
#endif
[CreateAssetMenu(fileName = "NPCSO", menuName = "Midnight Line SOs / NPC SO")]
public class PassengerData : ScriptableObject
{
    public PassengerBrain prefab;

    [TextArea(3,10)]public string offenceSentence;

    public Vector2 idleDurationRange = new Vector2(10, 30);

    public float moveSpeed = 1f;
    
    public int mugShotIndex;
    
    public Habits behaviours;
    public Gender gender;
    public Ethnicity ethnicity;

    [Header("Generated")]
    public float triggerRadius;

    private void OnValidate()
    {
        GenerateAutomatedData();
    }

#if UNITY_EDITOR
    public void GenerateAutomatedData()
    {
        AtlasSO passengerAtlas = prefab.atlasRenderer.atlas;
        passengerAtlas.UpdateClipDictionary();
        AtlasClip blinkingClip = passengerAtlas.clipDict[(int)PassengerMotion.StandingBlinking];
        MotionSprite firstBlinkingSprite = passengerAtlas.motionSprites[blinkingClip.keyframeStartIndex];
        triggerRadius = firstBlinkingSprite.sprite.worldSize.x * 0.5f;
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(PassengerData))]
public class PassengerDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        PassengerData passengerData = (PassengerData)target;

        GUIContent guiContent = new GUIContent("Generate Automated Data");

        if (GUILayout.Button(guiContent))
        {
            passengerData.GenerateAutomatedData();
        }
    }
}
#endif
