using UnityEngine;

[ExecuteAlways]
public class ColorPicker : MonoBehaviour
{
    public ColorsSO colorsSO;

    private void Start()
    {
        Shader.SetGlobalColor("_MainColor", colorsSO.mainColor);
        Shader.SetGlobalColor("_TicketCheckColor", colorsSO.ticketCheckColor);
        Shader.SetGlobalColor("_SuspicionColor", colorsSO.suspicionColor);
        Shader.SetGlobalColor("_RuledOutColor", colorsSO.ruledOutColor);
        Shader.SetGlobalFloat("_DayNight", colorsSO.dayNight);
        Shader.SetGlobalFloat("_DayNightFactor", colorsSO.dayNightFactor);
    }

    private void Update()
    {
#if UNITY_EDITOR
        Shader.SetGlobalColor("_MainColor", colorsSO.mainColor);
        Shader.SetGlobalColor("_TicketCheckColor", colorsSO.ticketCheckColor);
        Shader.SetGlobalColor("_SuspicionColor", colorsSO.suspicionColor);
        Shader.SetGlobalColor("_RuledOutColor", colorsSO.ruledOutColor);
        Shader.SetGlobalFloat("_DayNight", colorsSO.dayNight);
        Shader.SetGlobalFloat("_DayNightFactor", colorsSO.dayNightFactor);
#endif
    }
}
