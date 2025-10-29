using UnityEngine;

[CreateAssetMenu(fileName = "Carriage_SO", menuName = "Midnight Line SOs / Carriage SO")]
public class CarriageSO : ScriptableObject
{
    public Sprite wheelSprite;
    public Sprite slideDoorSprite;
    public float doorMovingTime = 2.0f;
    internal float degPerMeter;


}
