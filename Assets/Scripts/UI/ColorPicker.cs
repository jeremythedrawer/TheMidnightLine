using UnityEngine;

public class ColorPicker : MonoBehaviour
{
    public ColorsSO colorsSO;

    private void Start()
    {
        Shader.SetGlobalColor("_TicketCheckColor", colorsSO.ticketCheckColor);
        Shader.SetGlobalColor("_SuspicionColor", colorsSO.suspicionColor);
        Shader.SetGlobalColor("_RuledOutColor", colorsSO.ruledOutColor);
    }

    private void Update()
    {
#if UNITY_EDITOR
        Shader.SetGlobalColor("_TicketCheckColor", colorsSO.ticketCheckColor);
        Shader.SetGlobalColor("_SuspicionColor", colorsSO.suspicionColor);
        Shader.SetGlobalColor("_RuledOutColor", colorsSO.ruledOutColor);
#endif
    }
}
