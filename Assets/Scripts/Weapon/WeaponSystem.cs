// WeaponSystem.cs (Sistema completo con UI y compatibilidad con WeaponManager)
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using TMPro;

[System.Serializable]
public class WeaponStats
{
    public string weaponName = "Pistola";
    public float damage = 10f;
    public float fireRate = 0.2f; // Tiempo entre disparos en segundos
    public int magazineSize = 12;
    public float reloadTime = 1.5f;
    public float bulletSpeed = 50f;
    public float bulletLifetime = 3f;
    public float bulletSpread = 0.03f; // Dispersión base de las balas
    public bool isAutomatic = false;
    public GameObject bulletPrefab;
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;
    public ParticleSystem muzzleFlash;

    // Parámetros de retroceso
    public float recoilPositionForceVertical = 0.05f;   // Retroceso vertical (hacia atrás)
    public float recoilPositionForceHorizontal = 0.02f; // Retroceso horizontal (laterales)
    public float recoilRotationForceVertical = 2f;      // Rotación vertical (arriba)
    public float recoilRotationForceHorizontal = 1f;    // Rotación horizontal (lados)
    public float recoilPositionResetSpeed = 5f;         // Velocidad de recuperación de posición
    public float recoilRotationResetSpeed = 5f;         // Velocidad de recuperación de rotación
    public float recoilMaxPositionOffset = 0.3f;        // Desplazamiento máximo permitido
    public float recoilMaxRotation = 8f;                // Rotación máxima permitida

    // Parámetros de acumulación de retroceso
    public float recoilAccumulationFactor = 0.7f;       // Qué tanto se acumula el retroceso (0-1)
    public float recoilMaxAccumulation = 0.8f;          // Máximo factor de acumulación
    public float recoilDecayRate = 0.3f;                // Velocidad a la que disminuye la acumulación

    // UI settings
    public int lowAmmoThreshold = 3;                    // Umbral para considerar "pocas balas"
}

public class WeaponSystem : MonoBehaviour
{
    [Header("Referencias básicas")]
    [SerializeField] private WeaponStats weaponStats;
    [SerializeField] private Transform firePoint;
    [SerializeField] private PlayerControls playerControls;
    [SerializeField] private Camera playerCamera;

    [Header("UI Referencias")]
    [SerializeField] private TextMeshProUGUI ammoText;        // Texto para mostrar la munición
    [SerializeField] private TextMeshProUGUI weaponNameText;  // Texto para mostrar nombre del arma
    [SerializeField] private GameObject reloadingIndicator;   // Indicador de recarga


    private int currentAmmo;
    private bool isReloading = false;
    private float nextFireTime = 0f;
    private AudioSource audioSource;

    // Control de la corutina de recarga
    private Coroutine reloadCoroutine;

    // Variables para el sistema de retroceso
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 targetRecoilPosition;
    private Vector3 targetRecoilRotation;
    private Vector3 currentRecoilPosition;
    private Vector3 currentRecoilRotation;

    // Variables para la acumulación de retroceso
    private float recoilAccumulation = 0f;
    private float lastShotTime = 0f;
    private float effectiveSpread;

    // Variables para la UI de munición
    private bool isLowAmmo = false;
    private Coroutine lowAmmoBlinkCoroutine;
    private bool isActive = false;

    private void Awake()
    {
        if (playerControls == null)
            playerControls = new PlayerControls();

        if (playerCamera == null)
            playerCamera = Camera.main;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        currentAmmo = weaponStats.magazineSize;

        // Guardar la posición y rotación inicial del arma
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;

        // Inicializar variables de retroceso
        targetRecoilPosition = Vector3.zero;
        targetRecoilRotation = Vector3.zero;
        currentRecoilPosition = Vector3.zero;
        currentRecoilRotation = Vector3.zero;

        // Inicializar la dispersión efectiva
        effectiveSpread = weaponStats.bulletSpread;

        // Ya no es necesario crear el crosshair aquí
    }

    private void OnEnable()
    {
        isActive = true;
        playerControls.Player.Fire.performed += OnFire;
        playerControls.Player.Fire.canceled += OnFireReleased;
        playerControls.Player.Reload.performed += OnReload;
        playerControls.Player.Enable();

        // Actualizar la UI al activar el arma
        UpdateAmmoUI();
        UpdateWeaponNameUI();

        // Mostrar la UI
        SetUIVisibility(true);
    }

    private void OnDisable()
    {
        isActive = false;
        playerControls.Player.Fire.performed -= OnFire;
        playerControls.Player.Fire.canceled -= OnFireReleased;
        playerControls.Player.Reload.performed -= OnReload;
        playerControls.Player.Disable();

        // Cancelar recarga al desactivar
        CancelReload();

        // Al desactivar, detener cualquier parpadeo
        if (lowAmmoBlinkCoroutine != null)
        {
            StopCoroutine(lowAmmoBlinkCoroutine);
            lowAmmoBlinkCoroutine = null;
        }

        // Ocultar la UI
        SetUIVisibility(false);
    }

    private void Update()
    {
        if (!isActive) return;

        // Si el arma es automática y el botón de disparo está presionado, disparar continuamente
        if (weaponStats.isAutomatic && Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            TryShoot();
        }

        // Actualizar el sistema de retroceso
        UpdateRecoil();

        // Actualizar la acumulación de retroceso
        UpdateRecoilAccumulation();
    }

    private void OnFire(InputAction.CallbackContext context)
    {
        if (!weaponStats.isAutomatic)
        {
            TryShoot();
        }
    }

    private void OnFireReleased(InputAction.CallbackContext context)
    {
        // Para armas automáticas, se maneja en Update
    }

    private void OnReload(InputAction.CallbackContext context)
    {
        StartReload();
    }

    private void TryShoot()
    {
        if (isReloading) return;

        if (Time.time < nextFireTime) return;

        if (currentAmmo <= 0)
        {
            // Sin munición
            if (weaponStats.emptySound != null)
            {
                audioSource.PlayOneShot(weaponStats.emptySound);
            }
            StartReload();
            return;
        }

        // Disparar
        Shoot();

        // Aplicar retroceso
        ApplyRecoil();

        // Actualizar tiempo del último disparo para calcular la acumulación
        lastShotTime = Time.time;

        // Actualizar tiempo del próximo disparo
        nextFireTime = Time.time + weaponStats.fireRate;

        // Actualizar UI de munición
        UpdateAmmoUI();
    }

    private void Shoot()
    {
        // Reducir munición
        currentAmmo--;

        // Reproducir sonido de disparo
        if (weaponStats.shootSound != null)
        {
            audioSource.PlayOneShot(weaponStats.shootSound);
        }

        // Mostrar fogonazo
        if (weaponStats.muzzleFlash != null)
        {
            weaponStats.muzzleFlash.Play();
        }

        // Crear bala
        if (weaponStats.bulletPrefab != null)
        {
            // Calcular dirección con dispersión incrementada por el retroceso acumulado
            Vector3 direction = CalculateShootDirection();

            // Instanciar bala
            GameObject bullet = Instantiate(weaponStats.bulletPrefab, firePoint.position, Quaternion.LookRotation(direction));
            Bullet bulletComponent = bullet.GetComponent<Bullet>();

            if (bulletComponent != null)
            {
                bulletComponent.Initialize(direction, weaponStats.bulletSpeed, weaponStats.damage, weaponStats.bulletLifetime);
            }
            else
            {
                // Si no tiene componente Bullet, añadir física simple
                Rigidbody rb = bullet.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = direction * weaponStats.bulletSpeed;
                }

                // Destruir después del tiempo de vida
                Destroy(bullet, weaponStats.bulletLifetime);
            }
        }
    }

    private Vector3 CalculateShootDirection()
    {
        // Dirección base hacia el centro de la pantalla
        Vector3 direction = playerCamera.transform.forward;

        // Calcular dispersión basada en el retroceso acumulado
        float currentSpread = weaponStats.bulletSpread * (1f + recoilAccumulation * 5f);

        // Añadir dispersión
        if (currentSpread > 0)
        {
            direction += new Vector3(
                Random.Range(-currentSpread, currentSpread),
                Random.Range(-currentSpread, currentSpread),
                Random.Range(-currentSpread, currentSpread) * 0.1f // Menos dispersión en el eje Z
            );

            direction.Normalize();
        }

        // Guardar la dispersión efectiva actual para actualizar el crosshair
        effectiveSpread = currentSpread;

        return direction;
    }
    public void SetUIReferences(TextMeshProUGUI ammo, TextMeshProUGUI weaponName, GameObject reloadingInd)
    {
        // Asignar las nuevas referencias
        ammoText = ammo;
        weaponNameText = weaponName;
        reloadingIndicator = reloadingInd;

        // Actualizar inmediatamente los textos
        UpdateAmmoUI();
        UpdateWeaponNameUI();

        // Actualizar visibilidad del indicador de recarga según el estado actual
        if (reloadingIndicator != null)
        {
            reloadingIndicator.SetActive(isReloading);
        }

        // Mostrar la UI
        SetUIVisibility(true);

        // Si estaba en estado de pocas balas, restaurar el parpadeo
        if (isLowAmmo && lowAmmoBlinkCoroutine == null && ammoText != null)
        {
            lowAmmoBlinkCoroutine = StartCoroutine(BlinkLowAmmoText());
        }
    }
    public void StartReload()
    {
        if (isReloading || currentAmmo >= weaponStats.magazineSize) return;

        CancelReload();

        reloadCoroutine = StartCoroutine(ReloadCoroutine());
    }
    public void CancelReload()
    {
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }

        isReloading = false;

        // Ocultar indicador de recarga
        if (reloadingIndicator != null)
        {
            reloadingIndicator.SetActive(false);
        }
    }
    private IEnumerator ReloadCoroutine()
    {
        isReloading = true;

        // Actualizar UI de recarga
        if (reloadingIndicator != null)
        {
            reloadingIndicator.SetActive(true);
        }

        // Reproducir sonido de recarga
        if (weaponStats.reloadSound != null)
        {
            audioSource.PlayOneShot(weaponStats.reloadSound);
        }

        // Esperar tiempo de recarga
        yield return new WaitForSeconds(weaponStats.reloadTime);

        // Verificar si aún estamos recargando (el arma podría haber sido cambiada)
        if (isReloading)
        {
            // Recargar munición
            currentAmmo = weaponStats.magazineSize;

            // Actualizar UI
            UpdateAmmoUI();

            // Ocultar indicador de recarga
            if (reloadingIndicator != null)
            {
                reloadingIndicator.SetActive(false);
            }

            isReloading = false;
            reloadCoroutine = null;
        }
    }

    // Método para mostrar la munición actual (para UI)
    public string GetAmmoText()
    {
        return $"{currentAmmo}/{weaponStats.magazineSize}";
    }

    // Métodos para UI de arma
    private void UpdateAmmoUI()
    {
        if (ammoText == null) return;

        if (!ammoText.gameObject.activeInHierarchy) return;

        ammoText.text = GetAmmoText();

        bool isNowLowAmmo = currentAmmo <= weaponStats.lowAmmoThreshold && currentAmmo > 0;

        if (isNowLowAmmo && !isLowAmmo)
        {
            isLowAmmo = true;

            if (lowAmmoBlinkCoroutine != null)
            {
                StopCoroutine(lowAmmoBlinkCoroutine);
            }

            lowAmmoBlinkCoroutine = StartCoroutine(BlinkLowAmmoText());
        }
        else if (!isNowLowAmmo && isLowAmmo)
        {
            isLowAmmo = false;

            if (lowAmmoBlinkCoroutine != null)
            {
                StopCoroutine(lowAmmoBlinkCoroutine);
                lowAmmoBlinkCoroutine = null;
            }

            // Restaurar color normal
            ammoText.color = Color.white;
        }
        else if (currentAmmo <= 0)
        {
            if (lowAmmoBlinkCoroutine != null)
            {
                StopCoroutine(lowAmmoBlinkCoroutine);
                lowAmmoBlinkCoroutine = null;
            }

            ammoText.color = Color.red;
        }
    }

    private IEnumerator BlinkLowAmmoText()
    {
        while (isLowAmmo && isActive)
        {
            // Alternar entre rojo y blanco
            ammoText.color = Color.red;
            yield return new WaitForSeconds(0.5f);
            ammoText.color = Color.white;
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void UpdateWeaponNameUI()
    {
        if (weaponNameText != null)
        {
            weaponNameText.text = weaponStats.weaponName;
        }
    }

    private void SetUIVisibility(bool visible)
    {
        // Verificar que las referencias existen antes de intentar modificarlas
        if (ammoText != null)
        {
            // Verificar que el objeto padre (si existe) está activo
            if (ammoText.transform.parent != null && !ammoText.transform.parent.gameObject.activeSelf)
            {
                // No hacer nada si el padre está inactivo
                return;
            }

            // Activar/desactivar solo si es necesario cambiar el estado
            if (ammoText.gameObject.activeSelf != visible)
                ammoText.gameObject.SetActive(visible);
        }

        if (weaponNameText != null)
        {
            if (weaponNameText.transform.parent != null && !weaponNameText.transform.parent.gameObject.activeSelf)
            {
                return;
            }

            if (weaponNameText.gameObject.activeSelf != visible)
                weaponNameText.gameObject.SetActive(visible);
        }

        if (reloadingIndicator != null)
        {
            if (reloadingIndicator.transform.parent != null && !reloadingIndicator.transform.parent.gameObject.activeSelf)
            {
                return;
            }

            // Solo mostrar el indicador si estamos recargando Y la UI debe ser visible
            bool shouldBeVisible = visible && isReloading;
            if (reloadingIndicator.activeSelf != shouldBeVisible)
                reloadingIndicator.SetActive(shouldBeVisible);
        }
    }

    // Sistemas de retroceso y punto de mira dinámico

    // Aplicar el retroceso cuando se dispara
    private void ApplyRecoil()
    {
        // Factor de acumulación para hacer el retroceso más intenso con disparos consecutivos
        float recoilMultiplier = 1f + (recoilAccumulation * 3f);

        // Retroceso en posición (hacia atrás y lateralmente)
        float horizontalOffset = Random.Range(-1f, 1f) * weaponStats.recoilPositionForceHorizontal * recoilMultiplier;
        float verticalOffset = -weaponStats.recoilPositionForceVertical * recoilMultiplier; // Siempre hacia atrás

        targetRecoilPosition += new Vector3(
            horizontalOffset,
            Random.Range(-0.5f, 0.5f) * weaponStats.recoilPositionForceHorizontal * recoilMultiplier, // Ligero movimiento vertical
            verticalOffset
        );

        // Retroceso en rotación (principalmente hacia arriba, pero también lateralmente)
        float xRecoil = Random.Range(weaponStats.recoilRotationForceVertical * 0.7f, weaponStats.recoilRotationForceVertical) * recoilMultiplier;
        float yRecoil = Random.Range(-1f, 1f) * weaponStats.recoilRotationForceHorizontal * recoilMultiplier;
        float zRecoil = Random.Range(-0.5f, 0.5f) * weaponStats.recoilRotationForceHorizontal * recoilMultiplier * 0.5f; // Ligera inclinación

        targetRecoilRotation += new Vector3(xRecoil, yRecoil, zRecoil);

        // Limitar el retroceso máximo
        targetRecoilPosition = Vector3.ClampMagnitude(targetRecoilPosition, weaponStats.recoilMaxPositionOffset);

        targetRecoilRotation.x = Mathf.Clamp(targetRecoilRotation.x, -weaponStats.recoilMaxRotation, weaponStats.recoilMaxRotation);
        targetRecoilRotation.y = Mathf.Clamp(targetRecoilRotation.y, -weaponStats.recoilMaxRotation, weaponStats.recoilMaxRotation);
        targetRecoilRotation.z = Mathf.Clamp(targetRecoilRotation.z, -weaponStats.recoilMaxRotation * 0.5f, weaponStats.recoilMaxRotation * 0.5f);

        // Incrementar la acumulación de retroceso
        recoilAccumulation = Mathf.Min(
            recoilAccumulation + weaponStats.recoilAccumulationFactor * Time.deltaTime * 5f,
            weaponStats.recoilMaxAccumulation
        );
    }

    // Actualizar el retroceso en cada frame
    private void UpdateRecoil()
    {
        // Retorno gradual a la posición original
        targetRecoilPosition = Vector3.Lerp(targetRecoilPosition, Vector3.zero, weaponStats.recoilPositionResetSpeed * Time.deltaTime);
        targetRecoilRotation = Vector3.Lerp(targetRecoilRotation, Vector3.zero, weaponStats.recoilRotationResetSpeed * Time.deltaTime);

        // Actualizar la posición y rotación actuales con suavizado
        currentRecoilPosition = Vector3.Lerp(currentRecoilPosition, targetRecoilPosition, 10f * Time.deltaTime);
        currentRecoilRotation = Vector3.Lerp(currentRecoilRotation, targetRecoilRotation, 10f * Time.deltaTime);

        // Aplicar los cambios al arma
        transform.localPosition = originalPosition + currentRecoilPosition;
        transform.localRotation = originalRotation * Quaternion.Euler(currentRecoilRotation);
    }

    // Actualizar la acumulación de retroceso (decrece con el tiempo)
    private void UpdateRecoilAccumulation()
    {
        if (Time.time - lastShotTime > 0.1f) // Empezar a reducir después de un breve retraso
        {
            recoilAccumulation = Mathf.Max(
                recoilAccumulation - weaponStats.recoilDecayRate * Time.deltaTime,
                0f
            );
        }
    }

    public float GetCurrentRecoilAccumulation()
    {
        return recoilAccumulation;
    }

    public float GetEffectiveSpread()
    {
        return effectiveSpread;
    }
    // Método para reiniciar manualmente el retroceso acumulado
    public void ResetRecoil()
    {
        recoilAccumulation = 0f;
        targetRecoilPosition = Vector3.zero;
        targetRecoilRotation = Vector3.zero;
    }

    // Métodos adicionales para interacción con WeaponManager

    // Método para obtener el nombre del arma
    public string GetWeaponName()
    {
        return weaponStats.weaponName;
    }

    // Método para obtener el tamaño del cargador
    public int GetMagazineSize()
    {
        return weaponStats.magazineSize;
    }

    // Método para obtener la munición actual
    public int GetCurrentAmmo()
    {
        return currentAmmo;
    }

    // Método para comprobar si se está recargando
    public bool IsReloading()
    {
        return isReloading;
    }

    // Método para obtener las estadísticas del arma
    public WeaponStats GetWeaponStats()
    {
        return weaponStats;
    }
}