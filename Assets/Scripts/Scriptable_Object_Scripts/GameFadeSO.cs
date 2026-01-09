using UnityEngine;

[CreateAssetMenu(fileName = "GameFadeSO", menuName = "Midnight Line SOs / Game Fade SO")]
public class GameFadeSO : ScriptableObject
{
    public float fadeTime = 1;
    internal float brightness;
}
