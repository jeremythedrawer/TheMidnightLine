using System;
using UnityEngine;

[CreateAssetMenu(fileName = "TrainSettings_SO", menuName = "Midnight Line SOs / Train Settings SO")]
public class TrainSettingsSO : ScriptableObject
{
    public Sprite wheelSprite;
    public Sprite slideDoorSprite;
    public Sprite chairSprite;
    public float accelerationSpeed = 10f;
    public float doorMoveTime = 2.0f;
    public float exteriorWallFadeTime = 1f;
    [Serializable] public struct WorldZPosRange
    {
        public int max;
        public int min;
    }
    public WorldZPosRange maxMinWorldZPos;
}
