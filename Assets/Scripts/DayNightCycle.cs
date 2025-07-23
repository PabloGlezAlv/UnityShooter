using UnityEngine;
using System;

public class DayNightCycle : MonoBehaviour
{
    [Header("Luces del cielo")]
    [Tooltip("La luz direccional que representa el sol")]
    public Light sunLight;
    [Tooltip("La luz direccional que representa la luna")]
    public Light moonLight;

    [Header("Configuraci�n del ciclo")]
    [Tooltip("Duraci�n del d�a completo en segundos")]
    public float dayDuration = 600f; // 10 minutos por defecto

    [Range(0f, 1f)]
    [Tooltip("Hora del d�a actual (0-1)")]
    public float timeOfDay = 0.25f; // Comienza a las 6:00 am

    [Tooltip("Determina si el ciclo avanza autom�ticamente")]
    public bool cyclePaused = false;

    [Header("Configuraci�n del sol")]
    [Tooltip("Colores del cielo durante el d�a")]
    public Gradient sunColorGradient;

    [Tooltip("Intensidad de la luz solar durante el ciclo")]
    public AnimationCurve sunIntensityCurve;

    [Range(0f, 8f)]
    [Tooltip("Intensidad m�xima de la luz solar")]
    public float maxSunIntensity = 1f;

    [Header("Configuraci�n de la luna")]
    [Tooltip("Colores de la luna durante la noche")]
    public Gradient moonColorGradient;

    [Tooltip("Intensidad de la luz lunar durante el ciclo")]
    public AnimationCurve moonIntensityCurve;

    [Range(0f, 8f)]
    [Tooltip("Intensidad m�xima de la luz lunar")]
    public float maxMoonIntensity = 0.3f;

    [Header("Configuraci�n de Transici�n")]
    [Tooltip("Duraci�n de la transici�n d�a-noche en proporci�n del d�a (0-0.2)")]
    [Range(0.01f, 0.2f)]
    public float transitionDuration = 0.1f; // 10% del d�a

    [Tooltip("Hora del d�a en que comienza el atardecer (0-1)")]
    [Range(0.4f, 0.6f)]
    public float sunsetTime = 0.5f; // Medio d�a

    [Tooltip("Duraci�n del ciclo lunar en d�as")]
    public float lunarCycleDuration = 30f; // Duraci�n del ciclo lunar en d�as

    [Range(0f, 1f)]
    [Tooltip("Fase lunar inicial (0-1)")]
    public float initialLunarPhase = 0f; // 0 = Luna nueva, 0.25 = Cuarto creciente, 0.5 = Luna llena, 0.75 = Cuarto menguante

    [Tooltip("Objetos que representan las distintas fases de la luna")]
    public GameObject[] moonPhaseObjects;

    [Tooltip("�ngulo de desfase entre el sol y la luna (generalmente cercano a 180�)")]
    [Range(0f, 360f)]
    public float moonSunOffset = 180f;

    [Header("Estrellas y Cielo")]
    [Tooltip("Objeto que contiene las estrellas")]
    public GameObject starsObject;

    [Tooltip("Intensidad de las estrellas durante la noche")]
    public AnimationCurve starsIntensityCurve;

    [Header("Niebla y Ambiente")]
    [Tooltip("�Usar niebla para simular cambios atmosf�ricos?")]
    public bool useFog = true;

    [Tooltip("Colores de la niebla durante el ciclo")]
    public Gradient fogColorGradient;

    [Tooltip("Densidad de la niebla durante el ciclo")]
    public AnimationCurve fogDensityCurve;

    [Tooltip("Densidad m�xima de la niebla")]
    public float maxFogDensity = 0.05f;

    // Variables privadas para el ciclo
    private float sunInitialIntensity;
    private float moonInitialIntensity;
    private Quaternion sunStartRotation;
    private Quaternion moonStartRotation;
    private Color fogStartColor;
    private bool fogWasEnabled;
    private float elapsedDays = 0f;
    private float currentLunarPhase;
    private Material skyboxMaterial;

    void Start()
    {
        // Verificar si existe luz solar asignada
        if (sunLight == null)
        {
            // Buscar la luz direccional en la escena
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional && light.name.ToLower().Contains("sun"))
                {
                    sunLight = light;
                    break;
                }
            }

            if (sunLight == null)
            {
                Debug.LogWarning("No se ha encontrado luz solar. Creando una nueva luz direccional.");
                GameObject sunObj = new GameObject("Sun");
                sunLight = sunObj.AddComponent<Light>();
                sunLight.type = LightType.Directional;
                sunLight.shadows = LightShadows.Soft;
                sunObj.transform.parent = transform;
            }
        }

        // Crear o asignar luz lunar si no existe
        if (moonLight == null)
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional && light.name.ToLower().Contains("moon"))
                {
                    moonLight = light;
                    break;
                }
            }

            if (moonLight == null)
            {
                Debug.LogWarning("No se ha encontrado luz lunar. Creando una nueva luz direccional.");
                GameObject moonObj = new GameObject("Moon");
                moonLight = moonObj.AddComponent<Light>();
                moonLight.type = LightType.Directional;
                moonLight.shadows = LightShadows.Soft;
                moonObj.transform.parent = transform;
            }
        }

        // Guardar valores iniciales
        sunInitialIntensity = sunLight.intensity;
        if (moonLight != null)
        {
            moonInitialIntensity = moonLight.intensity;
            moonStartRotation = moonLight.transform.rotation;
        }
        sunStartRotation = sunLight.transform.rotation;

        // Inicializar fase lunar
        currentLunarPhase = initialLunarPhase;

        // Configurar niebla
        if (useFog)
        {
            fogWasEnabled = RenderSettings.fog;
            fogStartColor = RenderSettings.fogColor;
            RenderSettings.fog = true;
        }

        // Obtener material del skybox si existe
        if (RenderSettings.skybox != null)
        {
            skyboxMaterial = RenderSettings.skybox;
        }

        // Aplicar configuraci�n inicial
        UpdateLighting(timeOfDay);
    }

    void Update()
    {
        // Si el ciclo no est� pausado, avanzar el tiempo
        if (!cyclePaused)
        {
            float previousTime = timeOfDay;

            // Incrementar tiempo de d�a basado en la duraci�n del ciclo
            timeOfDay += Time.deltaTime / dayDuration;

            // Detectar si pasamos a un nuevo d�a
            if (timeOfDay >= 1f)
            {
                timeOfDay -= 1f;
                elapsedDays += 1f;

                // Actualizar fase lunar
                UpdateLunarPhase();
            }

            // Actualizar iluminaci�n
            UpdateLighting(timeOfDay);
        }
    }

    void UpdateLighting(float time)
    {
        // Rotaci�n del sol (360 grados en base al tiempo)
        sunLight.transform.rotation = sunStartRotation * Quaternion.Euler(new Vector3((time * 360f) - 90f, 0, 0));

        // Rotaci�n de la luna (con desfase respecto al sol)
        if (moonLight != null)
        {
            float moonTime = (time + 0.5f) % 1f; // Desfase de 180 grados por defecto
            float moonAngle = (moonTime * 360f) - 90f;

            // Aplicar desfase personalizado
            float offsetAngle = moonSunOffset - 180f; // Ajuste respecto al desfase predeterminado
            moonAngle += offsetAngle;

            moonLight.transform.rotation = moonStartRotation * Quaternion.Euler(new Vector3(moonAngle, 0, 0));
        }

        // Calcular los momentos de transici�n para un blend suave
        float sunriseStart = sunsetTime - 0.5f; // Medio d�a antes del atardecer
        if (sunriseStart < 0) sunriseStart += 1f;

        float sunriseEnd = sunriseStart + transitionDuration;
        if (sunriseEnd > 1f) sunriseEnd -= 1f;

        float sunsetEnd = (sunsetTime + transitionDuration) % 1f;

        // Determinar si estamos en una transici�n y calcular factores de blend
        float sunFactor = 1f;
        float moonFactor = 1f;

        // Factor del sol (amanecer: 0->1, atardecer: 1->0)
        if (time >= sunriseStart && time <= sunriseEnd)
        {
            // Amanecer (luna se desvanece, sol aparece)
            sunFactor = Mathf.InverseLerp(sunriseStart, sunriseEnd, time);
            moonFactor = 1f - sunFactor;
        }
        else if (time >= sunsetTime && time <= sunsetEnd)
        {
            // Atardecer (sol se desvanece, luna aparece)
            sunFactor = Mathf.InverseLerp(sunsetEnd, sunsetTime, time);
            moonFactor = 1f - sunFactor;
        }
        else if (time > sunriseEnd && time < sunsetTime)
        {
            // D�a pleno
            sunFactor = 1f;
            moonFactor = 0f;
        }
        else
        {
            // Noche plena
            sunFactor = 0f;
            moonFactor = 1f;
        }

        // Aplicar factores de blend a las intensidades base
        float baseSunIntensity = sunIntensityCurve.Evaluate(time) * maxSunIntensity;
        sunLight.intensity = baseSunIntensity * sunFactor;
        sunLight.color = sunColorGradient.Evaluate(time);

        if (moonLight != null)
        {
            float baseMoonIntensity = moonIntensityCurve.Evaluate(time) * maxMoonIntensity;

            // Ajustar intensidad seg�n la fase lunar (m�xima en luna llena)
            float phaseIntensityFactor = GetLunarPhaseIntensityFactor(currentLunarPhase);

            // Aplicar ambos factores: fase lunar y tiempo del d�a
            moonLight.intensity = baseMoonIntensity * phaseIntensityFactor * moonFactor;
            moonLight.color = moonColorGradient.Evaluate(time);

            // Actualizar visuales de la fase lunar
            UpdateMoonPhaseVisuals(currentLunarPhase);
        }

        // Actualizar estrellas con transici�n suave
        if (starsObject != null && starsIntensityCurve != null)
        {
            // Intensidad base de las estrellas seg�n la curva
            float baseStarsIntensity = starsIntensityCurve.Evaluate(time);

            // Aplicar factor de blend similar al de la luna (m�s visible en la noche)
            float starsIntensity = baseStarsIntensity * moonFactor;

            // Si el objeto de estrellas tiene un componente Renderer
            Renderer starsRenderer = starsObject.GetComponent<Renderer>();
            if (starsRenderer != null && starsRenderer.material != null)
            {
                Color currentColor = starsRenderer.material.GetColor("_EmissionColor");
                starsRenderer.material.SetColor("_EmissionColor", new Color(currentColor.r, currentColor.g, currentColor.b, starsIntensity));
            }

            // O si tiene un Light
            Light starsLight = starsObject.GetComponent<Light>();
            if (starsLight != null)
            {
                starsLight.intensity = starsIntensity;
            }
        }

        // Actualizar niebla si est� habilitada
        if (useFog)
        {
            if (fogColorGradient != null)
                RenderSettings.fogColor = fogColorGradient.Evaluate(time);

            if (fogDensityCurve != null)
                RenderSettings.fogDensity = fogDensityCurve.Evaluate(time) * maxFogDensity;
        }

        // Actualizar skybox si est� disponible
        if (skyboxMaterial != null)
        {
            // Esto asume que el skybox tiene una propiedad "_Exposure" para controlar el brillo
            if (skyboxMaterial.HasProperty("_Exposure"))
            {
                float dayExposure = 1.0f;
                float nightExposure = 0.1f;
                // Usar una mezcla de factores sol/luna para la exposici�n
                float exposure = Mathf.Lerp(nightExposure, dayExposure, sunFactor);
                skyboxMaterial.SetFloat("_Exposure", exposure);
            }
        }
    }

    // Actualiza la fase lunar basada en los d�as transcurridos
    void UpdateLunarPhase()
    {
        // Calcular la nueva fase lunar basada en la duraci�n del ciclo lunar
        currentLunarPhase = (initialLunarPhase + (elapsedDays / lunarCycleDuration)) % 1f;

        // Actualizar visuales de la fase lunar
        UpdateMoonPhaseVisuals(currentLunarPhase);
    }

    // Actualiza los objetos visuales de las fases lunares
    void UpdateMoonPhaseVisuals(float phase)
    {
        if (moonPhaseObjects == null || moonPhaseObjects.Length == 0)
            return;

        // Determinar qu� fase lunar mostrar
        int totalPhases = moonPhaseObjects.Length;
        int activePhase = Mathf.FloorToInt(phase * totalPhases) % totalPhases;

        // Activar solo la fase actual
        for (int i = 0; i < moonPhaseObjects.Length; i++)
        {
            if (moonPhaseObjects[i] != null)
                moonPhaseObjects[i].SetActive(i == activePhase);
        }
    }

    // Calcula el factor de intensidad basado en la fase lunar (m�xima en luna llena)
    float GetLunarPhaseIntensityFactor(float phase)
    {
        // La fase 0.5 es luna llena (m�xima luz)
        // La fase 0.0 es luna nueva (m�nima luz)

        // Transformar la fase para que el valor m�ximo sea en luna llena
        float offsetPhase = (phase + 0.5f) % 1f;

        // Calcular un factor sinusoidal (m�ximo en 0.5, m�nimo en 0.0 y 1.0)
        // Sen(x*PI) da un resultado de 0 a 1 y luego a 0 para x de 0 a 1
        return Mathf.Sin(offsetPhase * Mathf.PI);
    }

    // M�todos p�blicos para controlar el ciclo

    /// <summary>
    /// Establece la hora del d�a (0-1 donde 0=medianoche, 0.25=amanecer, 0.5=mediod�a, 0.75=atardecer)
    /// </summary>
    public void SetTimeOfDay(float time)
    {
        timeOfDay = Mathf.Clamp01(time);
        UpdateLighting(timeOfDay);
    }

    /// <summary>
    /// Pausa o reanuda el ciclo de d�a y noche
    /// </summary>
    public void TogglePause()
    {
        cyclePaused = !cyclePaused;
    }

    /// <summary>
    /// Establece la velocidad del ciclo cambiando la duraci�n del d�a
    /// </summary>
    public void SetCycleSpeed(float secondsPerDay)
    {
        dayDuration = Mathf.Max(1f, secondsPerDay);
    }

    /// <summary>
    /// Establece la fase lunar actual (0-1)
    /// </summary>
    public void SetLunarPhase(float phase)
    {
        currentLunarPhase = Mathf.Clamp01(phase);
        UpdateMoonPhaseVisuals(currentLunarPhase);
    }

    /// <summary>
    /// Establece la duraci�n de las transiciones d�a-noche
    /// </summary>
    public void SetTransitionDuration(float duration)
    {
        transitionDuration = Mathf.Clamp(duration, 0.01f, 0.2f);
    }

    /// <summary>
    /// Convierte la hora del d�a a formato de texto (HH:MM)
    /// </summary>
    public string GetTimeString()
    {
        // Convertir timeOfDay a horas y minutos
        float totalHours = timeOfDay * 24f;
        int hours = Mathf.FloorToInt(totalHours);
        int minutes = Mathf.FloorToInt((totalHours - hours) * 60f);

        return string.Format("{0:00}:{1:00}", hours, minutes);
    }

    /// <summary>
    /// Obtiene el nombre descriptivo de la fase lunar actual
    /// </summary>
    public string GetLunarPhaseName()
    {
        // Determinar nombre de la fase lunar seg�n el valor
        if (currentLunarPhase < 0.06f || currentLunarPhase > 0.94f)
            return "Luna Nueva";
        else if (currentLunarPhase < 0.19f)
            return "Luna Creciente";
        else if (currentLunarPhase < 0.31f)
            return "Cuarto Creciente";
        else if (currentLunarPhase < 0.44f)
            return "Gibosa Creciente";
        else if (currentLunarPhase < 0.56f)
            return "Luna Llena";
        else if (currentLunarPhase < 0.69f)
            return "Gibosa Menguante";
        else if (currentLunarPhase < 0.81f)
            return "Cuarto Menguante";
        else
            return "Luna Menguante";
    }

    // Al desactivar el componente, restaurar configuraci�n original
    void OnDisable()
    {
        if (sunLight != null)
        {
            sunLight.intensity = sunInitialIntensity;
        }

        if (moonLight != null)
        {
            moonLight.intensity = moonInitialIntensity;
        }

        if (useFog && fogWasEnabled)
        {
            RenderSettings.fog = fogWasEnabled;
            RenderSettings.fogColor = fogStartColor;
        }
    }
}