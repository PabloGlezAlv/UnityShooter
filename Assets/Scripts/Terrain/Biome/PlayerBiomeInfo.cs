using UnityEngine;

public class PlayerBiomeInfo : MonoBehaviour
{
    private Transform player;
    private BiomeSystem.BiomeData biomeInfo;
    private float checkInterval = 0.5f; // Intervalo para actualizar la informaci�n de bioma
    private float timeSinceLastCheck = 0f;
    private float gizmoSize = 1f;

    void Start()
    {
        player = transform;
        UpdateBiomeInfo();
    }

    void Update()
    {
        timeSinceLastCheck += Time.deltaTime;
        if (timeSinceLastCheck >= checkInterval)
        {
            UpdateBiomeInfo();
            timeSinceLastCheck = 0f;
        }
    }

    private void UpdateBiomeInfo()
    {
        // Obtener informaci�n de bioma para la posici�n actual del jugador
        biomeInfo = BiomeSystem.GetBiomeData(player.position);
    }

    private void OnDrawGizmos()
    {
        if (biomeInfo.biomeName == null)
            return;

#if UNITY_EDITOR
        // Dibuja un cubo para marcar la posici�n del jugador
        Gizmos.color = biomeInfo.biomeColor;
        Gizmos.DrawWireCube(player.position, Vector3.one * gizmoSize);

        // Mostrar la informaci�n del bioma encima del jugador
        UnityEditor.Handles.Label(player.position + Vector3.up * gizmoSize * 2.5f,
            $"Player Biome:\nTemp: {biomeInfo.temperature:F2}\nHumidity: {biomeInfo.humidity:F2}\nBioma: {biomeInfo.biomeName}");
#endif
    }
}