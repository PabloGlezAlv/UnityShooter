using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    // Referencias
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private float cameraSensitivity = 2.0f;

    // Configuración de movimiento
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private float runSpeed = 8.0f;
    [SerializeField] private float accelerationTime = 0.1f;
    [SerializeField] private float decelerationTime = 0.2f;
    [SerializeField] private AnimationCurve accelerationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve decelerationCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    // Configuración de salto y gravedad
    [SerializeField] private float gravity = -20f; // Gravedad más fuerte para saltos más responsivos
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float jumpCooldown = 0.1f; // Para evitar saltos accidentales repetidos

    // Configuración de dash
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 0.5f;
    [SerializeField] private float doubleTapTimeThreshold = 0.25f; // Tiempo máximo entre dobles pulsaciones

    // Sistema de armas
    [SerializeField] private WeaponSystem currentWeapon;

    // Variables internas
    private CharacterController characterController;
    private PlayerControls playerControls;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector3 verticalVelocity;
    private Vector3 currentVelocity; // Para el movimiento suavizado
    private Vector3 targetVelocity;
    private float xRotation = 0f;

    // Variables para control de salto
    private bool isJumpReady = true;

    // Variables para control de dash
    private bool isDashing = false;
    private bool canDash = true;
    private float lastTapTimeW = 0f;
    private float lastTapTimeS = 0f;
    private float lastTapTimeA = 0f;
    private float lastTapTimeD = 0f;
    private Vector3 dashDirection;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        // Inicializar el sistema de input
        playerControls = new PlayerControls();

        // Verificar que el cameraHolder esté asignado
        if (cameraHolder == null)
        {
            Debug.LogError("Camera Holder no asignado. Por favor, asigna el CameraHolder en el inspector.");
        }

        // Bloquear y ocultar el cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        // Suscribir los métodos a los eventos de input
        playerControls.Player.Move.performed += ctx => OnMoveInput(ctx.ReadValue<Vector2>());
        playerControls.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        playerControls.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        playerControls.Player.Look.canceled += ctx => lookInput = Vector2.zero;
        playerControls.Player.Jump.performed += OnJump;

        // Activar el Action Map
        playerControls.Player.Enable();
    }

    private void OnDisable()
    {
        // Desactivar el Action Map
        playerControls.Player.Disable();
    }

    private void OnMoveInput(Vector2 input)
    {
        moveInput = input;

        // Lógica para detectar doble tap en cada dirección
        float currentTime = Time.time;

        // Hacia adelante (W)
        if (input.y > 0.7f && input.x < 0.5f && input.x > -0.5f)
        {
            if (currentTime - lastTapTimeW < doubleTapTimeThreshold)
            {
                TryDash(transform.forward);
            }
            lastTapTimeW = currentTime;
        }

        // Hacia atrás (S)
        if (input.y < -0.7f && input.x < 0.5f && input.x > -0.5f)
        {
            if (currentTime - lastTapTimeS < doubleTapTimeThreshold)
            {
                TryDash(-transform.forward);
            }
            lastTapTimeS = currentTime;
        }

        // Hacia la izquierda (A)
        if (input.x < -0.7f && input.y < 0.5f && input.y > -0.5f)
        {
            if (currentTime - lastTapTimeA < doubleTapTimeThreshold)
            {
                TryDash(-transform.right);
            }
            lastTapTimeA = currentTime;
        }

        // Hacia la derecha (D)
        if (input.x > 0.7f && input.y < 0.5f && input.y > -0.5f)
        {
            if (currentTime - lastTapTimeD < doubleTapTimeThreshold)
            {
                TryDash(transform.right);
            }
            lastTapTimeD = currentTime;
        }
    }

    private void Update()
    {
        if (!isDashing)
        {
            HandleMovement();
        }
        HandleLook();
    }

    private void HandleMovement()
    {
        // Calcular dirección de movimiento deseada
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;

        // Normalizar vector si la magnitud es mayor a 1 (para evitar movimiento más rápido en diagonal)
        if (moveDirection.magnitude > 1f)
        {
            moveDirection.Normalize();
        }

        // Establecer la velocidad objetivo
        targetVelocity = moveDirection * moveSpeed;

        // Suavizar la transición de velocidad para un movimiento más fluido
        float accelerationRate = moveInput.sqrMagnitude > 0.1f ? accelerationTime : decelerationTime;
        AnimationCurve curve = moveInput.sqrMagnitude > 0.1f ? accelerationCurve : decelerationCurve;

        float t = Time.deltaTime / accelerationRate;
        float curveValue = curve.Evaluate(t);

        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, curveValue);

        // Aplicar movimiento horizontal
        characterController.Move(currentVelocity * Time.deltaTime);

        // Manejar gravedad y estado de estar en el suelo
        if (characterController.isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f; // Un pequeño valor negativo para mantener el personaje pegado al suelo
        }

        // Aplicar gravedad
        verticalVelocity.y += gravity * Time.deltaTime;
        characterController.Move(verticalVelocity * Time.deltaTime);
    }

    private void HandleLook()
    {
        // Aplicar sensibilidad y posible inversión de ejes
        float mouseX = lookInput.x * cameraSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * cameraSensitivity * Time.deltaTime;

        // Rotación vertical (para mirar arriba/abajo) - aplicada al holder de la cámara
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Limitar la rotación vertical
        cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Rotación horizontal (girar izquierda/derecha) - aplicada al jugador completo
        transform.Rotate(Vector3.up * mouseX);
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (characterController.isGrounded && isJumpReady)
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            isJumpReady = false;
            StartCoroutine(ResetJumpCooldown());
        }
    }

    private IEnumerator ResetJumpCooldown()
    {
        yield return new WaitForSeconds(jumpCooldown);
        isJumpReady = true;
    }

    private void TryDash(Vector3 direction)
    {
        if (canDash && !isDashing)
        {
            StartCoroutine(PerformDash(direction));
        }
    }

    private IEnumerator PerformDash(Vector3 direction)
    {
        isDashing = true;
        canDash = false;
        dashDirection = direction.normalized;

        // Guardar velocidad vertical para aplicarla después del dash
        float originalYVelocity = verticalVelocity.y;

        // Reducir gravedad durante el dash
        verticalVelocity.y = 0;

        float dashTimer = 0;

        while (dashTimer < dashDuration)
        {
            // Aplicar movimiento de dash
            characterController.Move(dashDirection * dashSpeed * Time.deltaTime);

            dashTimer += Time.deltaTime;
            yield return null;
        }

        // Restaurar velocidad vertical
        verticalVelocity.y = originalYVelocity;

        isDashing = false;

        // Esperar cooldown antes de permitir otro dash
        yield return new WaitForSeconds(dashCooldown);

        canDash = true;
    }

    public void EquipWeapon(WeaponSystem newWeapon)
    {
        currentWeapon = newWeapon;
    }
}