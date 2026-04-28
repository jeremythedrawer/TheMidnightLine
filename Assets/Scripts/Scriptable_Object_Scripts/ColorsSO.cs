using UnityEngine;

[CreateAssetMenu(fileName = "Colors", menuName = "Midnight Line SOs / Colors")]
public class ColorsSO : ScriptableObject
{
    public Color32 mainColor;

    public Color32 ticketCheckColor;
    public Color32 suspicionColor;
    public Color32 ruledOutColor;

    [Range(0,1)]public float dayNight;
    [Range(0,1)]public float dayNightFactor;

}
