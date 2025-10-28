using UnityEngine;

[CreateAssetMenu(fileName = "TrainSettings_SO", menuName = "Midnight Line SOs / Train Settings SO")]
public class TrainSettingsSO : ScriptableObject
{
    [SerializeField] float accelerationSpeed = 10f;
}
