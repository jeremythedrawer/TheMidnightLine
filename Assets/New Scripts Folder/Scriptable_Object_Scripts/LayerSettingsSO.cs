using UnityEngine;

[CreateAssetMenu(fileName = "LayerSettings_SO", menuName = "Midnight Line SOs / Layer Settings SO")]
public class LayerSettingsSO : ScriptableObject
{
    public LayerMask stationGround;
    public LayerMask trainGround;
    public LayerMask slideDoors;
    public LayerMask carriageChairs;
}
