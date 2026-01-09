using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Phone_SO", menuName = "Midnight Line SOs / Phone SO")]
public class PhoneSO : ScriptableObject
{
    public float interactDistance = 1f;
    public float ringDistance = 5f;
    public float minTutorialPartTime = 1f;
    [Serializable] public struct MaterialIDs
    {


    }
    internal MaterialIDs materialIDs;
}
