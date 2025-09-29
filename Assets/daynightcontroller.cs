using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Tijd instellingen")]
    [Range(0, 24)] public float timeOfDay = 12f; // Starttijd (0 = middernacht)
    public float dayDuration = 120f; // Hoeveel seconden duurt een volledige dag

    [Header("Licht instellingen")]
    public Light sun; // sleep je Directional Light hier naartoe
    public Gradient lightColor; // kleur van de zon afhankelijk van tijd
    public AnimationCurve lightIntensity; // intensiteit curve over de dag

    [Header("Fog instellingen")]
    public Color dayFog = new Color(0.7f, 0.8f, 0.9f);
    public Color nightFog = new Color(0.05f, 0.05f, 0.1f);
    [Range(0, 0.1f)] public float dayFogDensity = 0.002f;
    [Range(0, 0.1f)] public float nightFogDensity = 0.02f;

    [Header("Skybox instellingen")]
    public Material skyboxMaterial; // procedural skybox materiaal
    public float dayExposure = 1.3f;
    public float nightExposure = 0.3f;

    void Update()
    {
        UpdateTime();
        RotateSun();
        UpdateLighting();
    }

    void UpdateTime()
    {
        // Update tijd
        timeOfDay += (24 / dayDuration) * Time.deltaTime;
        if (timeOfDay >= 24) timeOfDay = 0;
    }

    void RotateSun()
    {
        // Bereken rotatie van de zon
        float sunRotation = (timeOfDay / 24f) * 360f;
        sun.transform.rotation = Quaternion.Euler(sunRotation - 90, 170, 0);
    }

    void UpdateLighting()
    {
        // Bereken daglicht factor (0 = nacht, 1 = dag)
        float dot = Mathf.Clamp01(Vector3.Dot(sun.transform.forward, Vector3.down));

        // Lichtkleur en intensiteit
        if (sun != null)
        {
            sun.color = lightColor.Evaluate(dot);
            sun.intensity = lightIntensity.Evaluate(dot);
        }

        // Fog kleur en dichtheid
        RenderSettings.fogColor = Color.Lerp(nightFog, dayFog, dot);
        RenderSettings.fogDensity = Mathf.Lerp(nightFogDensity, dayFogDensity, dot);

        // Skybox exposure
        if (skyboxMaterial != null)
        {
            float exposure = Mathf.Lerp(nightExposure, dayExposure, dot);
            skyboxMaterial.SetFloat("_Exposure", exposure);
        }
    }
}
