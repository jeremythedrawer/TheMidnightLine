using System;
using UnityEngine;

public class Station : MonoBehaviour
{
    [Serializable] public struct SOData
    {
        public StationSO station;
    }
    [SerializeField] public SOData soData;
}
