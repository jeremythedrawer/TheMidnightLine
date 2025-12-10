using UnityEngine;

[CreateAssetMenu(fileName = "SpyInputs_SO", menuName = "Midnight Line SOs / Spy Inputs SO")]
public class SpyInputsSO : ScriptableObject
{
    internal int move;
    internal bool jump;
    internal bool run;
    internal bool interact;
    internal int mouseScroll;

    internal Vector2 mouseScreenPos;
    internal Vector2 mouseWorldPos;
    internal bool mouseLeftDown;
    internal bool cancel;
}
