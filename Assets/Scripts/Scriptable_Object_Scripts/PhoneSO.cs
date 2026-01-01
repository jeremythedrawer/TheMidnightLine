using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Phone_SO", menuName = "Midnight Line SOs / Phone SO")]
public class PhoneSO : ScriptableObject
{
    public float interactDistance = 1f;
    [Serializable] public struct MaterialIDs
    {
        internal int hoveredID;
        internal int selectedID;
    }
    internal MaterialIDs materialIDs;
}
