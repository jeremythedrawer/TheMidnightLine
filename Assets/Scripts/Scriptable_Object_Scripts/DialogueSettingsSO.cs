using UnityEngine;


[CreateAssetMenu(fileName = "DialogueSettingsSO", menuName = "Midnight Line SOs / Dialogue Settings SO")]
public class DialogueSettingsSO : ScriptableObject
{
    public int characterDelayMS = 30;
    public float speechBoxGrowTime = 0.25f;
    public float paddingX = 20f;
    public float paddingY = 20f;
}
