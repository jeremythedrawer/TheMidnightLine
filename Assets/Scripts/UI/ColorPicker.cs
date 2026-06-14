using UnityEngine;

[ExecuteAlways]
public class ColorPicker : MonoBehaviour
{
    public ColorsSO colorsSO;
    private void Start()
    {
        Shader.SetGlobalColor("_BlackColor", colorsSO.blackColor);
        Shader.SetGlobalColor("_WhiteColor", colorsSO.whiteColor);
        Shader.SetGlobalColor("_TicketCheckColor", colorsSO.ticketCheckColor);
        Shader.SetGlobalFloat("_DayNight", colorsSO.dayNight);
        Shader.SetGlobalFloat("_DayNightFactor", colorsSO.dayNightFactor);
    }

    private void Update()
    {
#if UNITY_EDITOR
        Shader.SetGlobalColor("_BlackColor", colorsSO.blackColor);
        Shader.SetGlobalColor("_WhiteColor", colorsSO.whiteColor);
        Shader.SetGlobalColor("_TicketCheckColor", colorsSO.ticketCheckColor);

        Shader.SetGlobalFloat("_DayNightFactor", colorsSO.dayNightFactor);
#endif
        Shader.SetGlobalFloat("_DayNight", colorsSO.dayNight);
    }
}
