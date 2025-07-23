using UnityEngine;
using System.Linq;

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
        if (biomeInfo.influences == null || biomeInfo.influences.Count == 0)
            return;

#if UNITY_EDITOR
        var mainBiomeInfluence = biomeInfo.influences.OrderByDescending(i => i.influence).First();
        var mainBiome = BiomeSystem.GetAllBiomes().Find(b => b.name == mainBiomeInfluence.biomeName);

        if (mainBiome != null)
        {
            // Dibuja un cubo para marcar la posicin del jugador
            Gizmos.color = mainBiome.biomeColor;
            Gizmos.DrawWireCube(player.position, Vector3.one * gizmoSize);

            // Construir el texto de la etiqueta
            string labelText = $"Player Biome: {mainBiome.name} ({(mainBiomeInfluence.influence * 100):F1}%)\n";
            labelText += $"Temp: {biomeInfo.temperature:F2}, Hum: {biomeInfo.humidity:F2}\n";
            labelText += "Influences:\n";
            foreach (var influence in biomeInfo.influences.OrderByDescending(i => i.influence))
            {
                labelText += $"- {influence.biomeName}: {(influence.influence * 100):F1}%\n";
            }

            // Mostrar la informacin del bioma encima del jugador
            UnityEditor.Handles.Label(player.position + Vector3.up * gizmoSize * 2.5f, labelText);
        }
#endif
    }
}