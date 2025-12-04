using UnityEngine;

[CreateAssetMenu(fileName = "TrainSettings_SO", menuName = "Midnight Line SOs / Train Settings SO")]
public class TrainSettingsSO : ScriptableObject
{
    public StationSO[] stations;

    public Sprite wheelSprite;
    public Sprite slideDoorSprite;
    public float accelerationSpeed = 10f;
    public float doorMoveTime = 2.0f;
    public Vector2Int entityDepthRange = new Vector2Int(1, 5);
}
