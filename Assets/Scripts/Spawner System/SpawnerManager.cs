using UnityEngine;
using System.Collections.Generic;
public class SpawnerManager : MonoBehaviour
{
    private List<Spawner> spawners = new List<Spawner>();
    void Start()
    {
        spawners.AddRange(FindObjectsByType<Spawner>(FindObjectsSortMode.None));
    }
}
