using UnityEngine;

/// <summary>
/// Controla el movimiento de un avión en modo arcade (sin físicas).
/// El forward del modelo se asume que es -transform.right.
/// </summary>
public class PlaneController : MonoBehaviour
{
    [Header("Arcade Movement Settings")]
    [SerializeField] private float moveSpeed = 50f;
    [SerializeField] private float turnSpeed = 120f;
    [SerializeField] private float verticalSpeed = 30f;

    [Header("Camera Settings")]
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 5, 12);
    [SerializeField] private float cameraFollowSpeed = 10f;

    private Camera planeCamera;
    private GameObject cameraHolder;

    private float throttleInput; // W/S
    private float pitchInput;    // Mouse Y
    private float yawInput;      // Mouse X + A/D

    void Start()
    {
        // Busca o crea la cámara del avión
        var cam = GameObject.Find("PlaneCamera");
        if (cam == null)
        {
            CreatePlaneCamera();
        }
        else
        {
            cameraHolder = cam;
            planeCamera = cam.GetComponent<Camera>();
        }

        // Bloquear el cursor para un mejor control con el ratón
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    [ContextMenu("Create Plane Camera")]
    void CreatePlaneCamera()
    {
        cameraHolder = new GameObject("PlaneCamera");
        planeCamera = cameraHolder.AddComponent<Camera>();
        planeCamera.tag = "MainCamera";
        Debug.Log("Cámara del avión creada.");
    }

    void Update()
    {
        GetInputs();
        HandleMovement();
        UpdateCamera();
    }

    void GetInputs()
    {
        // Inputs para movimiento y rotación
        throttleInput = Input.GetAxis("Vertical");      // W/S
        pitchInput = -Input.GetAxis("Mouse Y");         // Mouse Y
        // Combina el ratón y las teclas A/D para girar
        yawInput = Input.GetAxis("Mouse X") + Input.GetAxis("Horizontal");
    }

    void HandleMovement()
    {
        // --- TRANSLATION (MOVIMIENTO) ---

        // Movimiento hacia adelante/atrás (W/S)
        // Recordar: El forward de este modelo es -transform.right
        transform.position += -transform.right * throttleInput * moveSpeed * Time.deltaTime;

        // Movimiento vertical (E/Q)
        if (Input.GetKey(KeyCode.E))
        {
            transform.position += Vector3.up * verticalSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            transform.position -= Vector3.up * verticalSpeed * Time.deltaTime;
        }

        // --- ROTATION (ROTACIÓN) ---

        // Yaw (Girar a los lados con A/D y Ratón X)
        transform.Rotate(Vector3.up, yawInput * turnSpeed * Time.deltaTime, Space.World);

        // Pitch (Inclinar arriba/abajo con Ratón Y)
        // El eje de pitch para este modelo (alas) es transform.forward
        transform.Rotate(transform.forward, pitchInput * turnSpeed * Time.deltaTime, Space.Self);
    }

    void UpdateCamera()
    {
        if (cameraHolder != null)
        {
            // La cámara se posiciona relativa al avión
            Vector3 targetPosition = transform.position + (transform.rotation * cameraOffset);
            cameraHolder.transform.position = Vector3.Lerp(cameraHolder.transform.position, targetPosition, Time.deltaTime * cameraFollowSpeed);
            
            // La cámara siempre mira hacia el morro del avión (-transform.right)
            Vector3 lookAtPoint = transform.position - transform.right * 10f;
            cameraHolder.transform.LookAt(lookAtPoint, Vector3.up);
        }
    }

    void OnDrawGizmosSelected()
    {
        // --- DIBUJAR VECTORES DE DEBUG ---
        // El FORWARD de este modelo es -RIGHT (ROJO)
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, -transform.right * 10f);

        // El eje de las alas es FORWARD (AZUL)
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 5f);

        // Vector UP (VERDE)
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.up * 5f);
    }
}
