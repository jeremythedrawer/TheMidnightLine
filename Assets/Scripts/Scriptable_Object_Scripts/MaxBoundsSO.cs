using UnityEngine;

[CreateAssetMenu(fileName = "MaxBoundsStatsSO", menuName = "Midnight Line SOs / Max Bounds Stats SO")]
public class MaxBoundsSO : ScriptableObject
{
    public float bufferAmount = 16f;
    internal Vector2 min;
    internal Vector2 max;
}
