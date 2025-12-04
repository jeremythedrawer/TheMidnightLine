using System;
using TMPro;
using UnityEngine;

public class ProfilePage : MonoBehaviour
{
    [Serializable] public struct ComponentData
    {
        public TMP_Text behaviours;
        public TMP_Text appearences;
    }
    [SerializeField] ComponentData components;
}
