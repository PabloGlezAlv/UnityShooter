// FPSController.cs (actualizado)
using System;
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
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 1.2f;

    // Sistema de armas
    [SerializeField] private WeaponSystem currentWeapon;

    // Variables internas
    private CharacterController characterController;
    private PlayerControls playerControls;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector3 verticalVelocity;
    private float xRotation = 0f;

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
        playerControls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
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

    private void Update()
    {
        HandleMovement();
        HandleLook();
    }

    private void HandleMovement()
    {
        // Calcular dirección de movimiento
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;

        // Aplicar gravedad
        if (characterController.isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f; // Un pequeño valor negativo para mantener el personaje pegado al suelo
        }

        // Aplicar movimiento horizontal
        characterController.Move(moveDirection * moveSpeed * Time.deltaTime);

        // Aplicar movimiento vertical (gravedad)
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
        if (characterController.isGrounded)
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    public void EquipWeapon(WeaponSystem newWeapon)
    {
        currentWeapon = newWeapon;
    }
}