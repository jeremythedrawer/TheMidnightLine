using UnityEngine;

public class SpawnedPrefab : MonoBehaviour
{
    [Header ("References")]
    public ParallaxController parallaxController;
    public virtual void Initialize()
    {
        parallaxController.Initialize();
    }
}
