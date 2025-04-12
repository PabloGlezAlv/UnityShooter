// WeaponManager.cs - Sistema completo con gestión de armas y UI
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class WeaponManager : MonoBehaviour
{
    [Header("Referencias de Armas")]
    [SerializeField] private List<GameObject> weaponPrefabs = new List<GameObject>();
    private List<WeaponSystem> instantiatedWeapons = new List<WeaponSystem>();
    [SerializeField] private Transform weaponParent; // Añadir para tener un punto donde instanciar las armas
    [SerializeField] private FPSController playerController;
    [SerializeField] private PlayerControls playerControls;

    [Header("Referencias UI")]
    [SerializeField] private RectTransform crosshairContainer; 
    [SerializeField]
    private Image crosshairCenter;
    [SerializeField]
    private RectTransform[] crosshairLines = new RectTransform[4]; // arriba, derecha, abajo, izquierda
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private GameObject reloadingIndicator;
    [SerializeField] private float weaponSwitchDelay = 0.3f; // Tiempo para la animación de cambio de arma

    [Header("Animación de Cambio")]
    [SerializeField] private float weaponSwitchLowerAmount = 0.5f; // Cuánto baja el arma al cambiar
    [SerializeField] private float weaponSwitchSpeed = 10f; // Velocidad de la animación de cambio

    private int currentWeaponIndex = 0;
    private bool isSwitchingWeapon = false;
    private Vector3 originalWeaponPosition;

    private void Awake()
    {
        if (playerControls == null)
            playerControls = new PlayerControls();

        if (playerController == null)
            playerController = GetComponentInParent<FPSController>();
    }

    private void Start()
    {
        // Instanciar armas desde prefabs
        foreach (GameObject prefab in weaponPrefabs)
        {
            if (prefab != null)
            {
                GameObject weaponInstance = Instantiate(prefab, weaponParent);
                WeaponSystem weaponSystem = weaponInstance.GetComponent<WeaponSystem>();

                if (weaponSystem != null)
                {
                    instantiatedWeapons.Add(weaponSystem);
                    weaponSystem.gameObject.SetActive(false);
                }
            }
        }

        // Desactivar el indicador de recarga inicialmente
        if (reloadingIndicator != null)
        {
            reloadingIndicator.SetActive(false);
        }

        // Activar arma inicial si hay alguna
        if (instantiatedWeapons.Count > 0)
        {
            EquipWeapon(0);
        }
    }

    private void OnEnable()
    {
        // Configura las teclas para cambiar de arma
        playerControls.Player.NextWeapon.performed += OnNextWeapon;
        playerControls.Player.PreviousWeapon.performed += OnPreviousWeapon;

        // Configurar el toggle para la rueda de armas (si existe esta acción)
        //if (playerControls.Player.WeaponWheel != null)
        //{
        //    playerControls.Player.WeaponWheel.performed += OnWeaponWheelToggle;
        //    playerControls.Player.WeaponWheel.canceled += OnWeaponWheelToggle;
        //}

        playerControls.Player.Enable();
    }

    private void OnDisable()
    {
        playerControls.Player.NextWeapon.performed -= OnNextWeapon;
        playerControls.Player.PreviousWeapon.performed -= OnPreviousWeapon;

        //if (playerControls.Player.WeaponWheel != null)
        //{
        //    playerControls.Player.WeaponWheel.performed -= OnWeaponWheelToggle;
        //    playerControls.Player.WeaponWheel.canceled -= OnWeaponWheelToggle;
        //}

        playerControls.Player.Disable();
    }

    private void Update()
    {
        // Si la rueda de armas está activa, comprobar selección
        //if (weaponWheelActive && weaponWheelUI != null)
        //{
        //    CheckWeaponWheelSelection();
        //}

        // Actualizar la UI si el arma actual está activa
        if (instantiatedWeapons.Count > 0 && currentWeaponIndex < instantiatedWeapons.Count)
        {
            WeaponSystem currentWeapon = instantiatedWeapons[currentWeaponIndex];
            if (currentWeapon.gameObject.activeInHierarchy)
            {
                UpdateWeaponUI(currentWeapon);

                // Actualizar el crosshair
                UpdateCrosshair();
            }
        }
    }

    private void OnNextWeapon(InputAction.CallbackContext context)
    {
        if (instantiatedWeapons.Count <= 1 || isSwitchingWeapon) return;
        int newIndex = (currentWeaponIndex + 1) % instantiatedWeapons.Count;
        StartCoroutine(SwitchWeaponWithAnimation(newIndex));
    }

    private void OnPreviousWeapon(InputAction.CallbackContext context)
    {
        if (instantiatedWeapons.Count <= 1 || isSwitchingWeapon) return;
        int newIndex = (currentWeaponIndex - 1 + instantiatedWeapons.Count) % instantiatedWeapons.Count;
        StartCoroutine(SwitchWeaponWithAnimation(newIndex));
    }

    private void SelectWeapon(int index)
    {
        if (index < instantiatedWeapons.Count && !isSwitchingWeapon)
        {
            StartCoroutine(SwitchWeaponWithAnimation(index));
        }
    }

    private IEnumerator SwitchWeaponWithAnimation(int newIndex)
    {
        isSwitchingWeapon = true;

        // Si hay un arma actual, cancelar su recarga y animarla hacia abajo
        if (currentWeaponIndex < instantiatedWeapons.Count && instantiatedWeapons[currentWeaponIndex] != null)
        {
            WeaponSystem currentWeapon = instantiatedWeapons[currentWeaponIndex];
            // Cancelar cualquier recarga en progreso
            currentWeapon.CancelReload();

            Transform weaponTransform = currentWeapon.transform;
            Vector3 initPos = weaponTransform.localPosition;
            Vector3 targetPos = initPos + Vector3.down * weaponSwitchLowerAmount;

            float elapTime = 0f;
            while (elapTime < weaponSwitchDelay * 0.5f)
            {
                weaponTransform.localPosition = Vector3.Lerp(initPos, targetPos, elapTime / (weaponSwitchDelay * 0.5f));
                elapTime += Time.deltaTime;
                yield return null;
            }

            // Desactivar el arma actual
            currentWeapon.gameObject.SetActive(false);
        }

        // Cambiar índice
        currentWeaponIndex = newIndex;

        // Activar la nueva arma pero comenzando desde abajo
        WeaponSystem newWeapon = instantiatedWeapons[currentWeaponIndex];
        newWeapon.gameObject.SetActive(true);

        // Configurar primero las referencias UI antes de cualquier otra operación
        newWeapon.SetUIReferences(ammoText, weaponNameText, reloadingIndicator);

        // Guardar la posición original
        originalWeaponPosition = newWeapon.transform.localPosition;

        // Establecer posición inicial abajo
        Vector3 startPos = originalWeaponPosition + Vector3.down * weaponSwitchLowerAmount;
        newWeapon.transform.localPosition = startPos;

        // Animar hacia arriba hasta la posición original
        float elapsedTime = 0f;
        while (elapsedTime < weaponSwitchDelay * 0.5f)
        {
            newWeapon.transform.localPosition = Vector3.Lerp(startPos, originalWeaponPosition, elapsedTime / (weaponSwitchDelay * 0.5f));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Asegurar que queda en la posición exacta
        newWeapon.transform.localPosition = originalWeaponPosition;

        // Informar al controlador del nuevo arma
        if (playerController != null)
        {
            playerController.EquipWeapon(newWeapon);
        }


        isSwitchingWeapon = false;
    }

    // Método para quitar un arma del inventario
    public void RemoveWeapon(WeaponSystem weapon)
    {
        int index = instantiatedWeapons.IndexOf(weapon);
        if (index != -1)
        {
            // Si el arma a eliminar es la actual, cambiar a otra primero
            if (index == currentWeaponIndex && instantiatedWeapons.Count > 1)
            {
                int newIndex = (index + 1) % instantiatedWeapons.Count;
                EquipWeapon(newIndex);
            }

            // Destruir el GameObject del arma
            Destroy(weapon.gameObject);

            // Remover de la lista
            instantiatedWeapons.RemoveAt(index);

            // Actualizar el índice actual si es necesario
            if (instantiatedWeapons.Count > 0)
            {
                currentWeaponIndex = Mathf.Clamp(currentWeaponIndex, 0, instantiatedWeapons.Count - 1);
            }
            else
            {
                currentWeaponIndex = 0;

                // Limpiar la UI si no quedan armas
                ClearWeaponUI();
            }
        }
    }


    // Método directo para equipar un arma sin animación
    private void EquipWeapon(int index)
    {
        // Desactivar arma actual y cancelar cualquier recarga
        if (currentWeaponIndex < instantiatedWeapons.Count && instantiatedWeapons[currentWeaponIndex] != null)
        {
            WeaponSystem currentWeapon = instantiatedWeapons[currentWeaponIndex];
            currentWeapon.CancelReload();
            currentWeapon.gameObject.SetActive(false);
        }

        // Cambiar índice
        currentWeaponIndex = index;

        // Activar nueva arma
        if (currentWeaponIndex < instantiatedWeapons.Count)
        {
            WeaponSystem newWeapon = instantiatedWeapons[currentWeaponIndex];
            newWeapon.gameObject.SetActive(true);

            // Establecer referencias UI primero
            newWeapon.SetUIReferences(ammoText, weaponNameText, reloadingIndicator);

            // Informar al controlador
            if (playerController != null)
            {
                playerController.EquipWeapon(newWeapon);
            }
        }
    }


    // Actualizar la UI con la información del arma actual
    private void UpdateWeaponUI(WeaponSystem weapon)
    {
        if (weapon == null) return;

        // Actualizar texto de munición
        if (ammoText != null)
        {
            ammoText.text = weapon.GetAmmoText();

            // Cambiar color según nivel de munición
            if (weapon.GetCurrentAmmo() <= 0)
            {
                ammoText.color = Color.red;
            }
            else if (weapon.GetCurrentAmmo() <= weapon.GetWeaponStats().lowAmmoThreshold)
            {
                // Para el parpadeo, se maneja dentro de WeaponSystem
                // Solo actualizamos si el color debe ser rojo fijo
                if (ammoText.color != Color.red)
                {
                    ammoText.color = Color.yellow;
                }
            }
            else
            {
                ammoText.color = Color.white;
            }
        }

        // Actualizar nombre del arma
        if (weaponNameText != null)
        {
            weaponNameText.text = weapon.GetWeaponName();
        }

        // Actualizar indicador de recarga
        if (reloadingIndicator != null)
        {
            reloadingIndicator.SetActive(weapon.IsReloading());
        }
    }

    // Limpiar la UI cuando no hay armas equipadas
    private void ClearWeaponUI()
    {
        if (ammoText != null)
        {
            ammoText.text = "0/0";
            ammoText.color = Color.white;
        }

        if (weaponNameText != null)
        {
            weaponNameText.text = "Sin arma";
        }

        if (reloadingIndicator != null)
        {
            reloadingIndicator.SetActive(false);
        }
    }


    // Mostrar notificación al obtener una nueva arma
    private IEnumerator ShowNewWeaponNotification(string weaponName)
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) yield break;

        GameObject notifObj = new GameObject("NewWeaponNotification");
        notifObj.transform.SetParent(canvas.transform, false);

        // Fondo de la notificación
        Image notifBg = notifObj.AddComponent<Image>();
        notifBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        RectTransform notifRect = notifObj.GetComponent<RectTransform>();
        notifRect.anchorMin = new Vector2(0.5f, 0);
        notifRect.anchorMax = new Vector2(0.5f, 0);
        notifRect.sizeDelta = new Vector2(400, 80);
        notifRect.anchoredPosition = new Vector2(0, -100); // Comienza fuera de la pantalla

        // Texto de la notificación
        GameObject textObj = new GameObject("NotificationText");
        textObj.transform.SetParent(notifObj.transform, false);

        TextMeshProUGUI notifText = textObj.AddComponent<TextMeshProUGUI>();
        notifText.text = $"NUEVA ARMA: {weaponName}";
        notifText.fontSize = 24;
        notifText.color = Color.white;
        notifText.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        // Animación de entrada
        float elapsedTime = 0f;
        float animDuration = 0.5f;
        while (elapsedTime < animDuration)
        {
            float t = elapsedTime / animDuration;
            notifRect.anchoredPosition = Vector2.Lerp(new Vector2(0, -100), new Vector2(0, 100), t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Mantener visible
        yield return new WaitForSeconds(3f);

        // Animación de salida
        elapsedTime = 0f;
        while (elapsedTime < animDuration)
        {
            float t = elapsedTime / animDuration;
            notifRect.anchoredPosition = Vector2.Lerp(new Vector2(0, 100), new Vector2(0, -100), t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(notifObj);
    }

    // Otros métodos para obtener información del arma actual (útil para otros sistemas)

    public WeaponSystem GetCurrentWeapon()
    {
        if (instantiatedWeapons.Count > 0 && currentWeaponIndex < instantiatedWeapons.Count)
        {
            return instantiatedWeapons[currentWeaponIndex];
        }
        return null;
    }

    public int GetWeaponCount()
    {
        return instantiatedWeapons.Count;
    }

    public bool HasWeapon(string weaponName)
    {
        foreach (WeaponSystem weapon in instantiatedWeapons)
        {
            if (weapon.GetWeaponName() == weaponName)
            {
                return true;
            }
        }
        return false;
    }

    // Método para actualizar la munición máxima (útil con power-ups)
    public void UpdateMaxAmmo(int additionalAmmo)
    {
        WeaponSystem currentWeapon = GetCurrentWeapon();
        if (currentWeapon != null)
        {
            WeaponStats stats = currentWeapon.GetWeaponStats();
            stats.magazineSize += additionalAmmo;

            // Actualizar UI
            UpdateWeaponUI(currentWeapon);
        }
    }

    private void UpdateCrosshair()
    {
        // Verificación más robusta
        if (crosshairContainer == null) return;
        if (crosshairLines == null) return;
        if (crosshairLines.Length < 4) return;
        if (crosshairCenter == null) return;

        // Comprobar cada línea individualmente
        bool allLinesValid = true;
        for (int i = 0; i < 4; i++)
        {
            if (crosshairLines[i] == null)
            {
                allLinesValid = false;
                break;
            }
        }

        if (!allLinesValid) return;

        // Obtener la información de retroceso del arma actual
        WeaponSystem currentWeapon = GetCurrentWeapon();
        if (currentWeapon == null) return;

        float recoilAccumulation = currentWeapon.GetCurrentRecoilAccumulation();
        float effectiveSpread = currentWeapon.GetEffectiveSpread();

        // Calcular el factor de expansión basado en la dispersión efectiva y el retroceso acumulado
        float expansionFactor = 1f + (recoilAccumulation * 15f) + (effectiveSpread * 100f);

        // Posiciones base para cada línea (arriba, derecha, abajo, izquierda)
        Vector2[] baseOffsets = { new Vector2(0, 8), new Vector2(8, 0), new Vector2(0, -8), new Vector2(-8, 0) };

        // Actualizar la posición de cada línea
        for (int i = 0; i < 4; i++)
        {
            // Aplicar el factor de expansión a la posición base
            Vector2 expandedOffset = baseOffsets[i] * expansionFactor;
            crosshairLines[i].anchoredPosition = expandedOffset;

            // También podemos ajustar el tamaño de las líneas si es necesario
            Vector2 currentSize = crosshairLines[i].sizeDelta;
            if (i % 2 == 0) // Líneas verticales (arriba y abajo)
            {
                crosshairLines[i].sizeDelta = new Vector2(2, 10 * (1f + recoilAccumulation * 0.5f));
            }
            else // Líneas horizontales (izquierda y derecha)
            {
                crosshairLines[i].sizeDelta = new Vector2(10 * (1f + recoilAccumulation * 0.5f), 2);
            }

            // Obtener y actualizar la imagen de la línea
            Image lineImage = crosshairLines[i].GetComponent<Image>();
            if (lineImage != null)
            {
                Color crosshairColor = Color.Lerp(Color.white, Color.red, recoilAccumulation);
                lineImage.color = new Color(crosshairColor.r, crosshairColor.g, crosshairColor.b, 0.8f);
            }
        }

        // Cambiar el color basado en el retroceso (más rojo = más retroceso)
        Color centerColor = Color.Lerp(Color.white, Color.red, recoilAccumulation);
        crosshairCenter.color = centerColor;
    }
}