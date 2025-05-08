using UnityEngine;

public class InteractableItem : MonoBehaviour
{
    [Header("Item Configuration")]
    [SerializeField] private string itemId;
    [SerializeField] private string itemName;
    [SerializeField] private Sprite itemIcon;
    [SerializeField] private string itemDescription;
    [SerializeField] private InventorySystem.ItemType itemType;
    [SerializeField] private int quantity = 1;

    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private string interactionPrompt = "Presiona E para recoger";

    private bool isInRange = false;
    private Camera playerCamera;

    void Start()
    {
        playerCamera = Camera.main;
    }

    void Update()
    {
        CheckPlayerDistance();

        if (isInRange)
        {
            // Mostrar prompt de interacci�n
            ShowInteractionPrompt();

            // Detectar si el jugador presiona la tecla de interacci�n
            if (Input.GetKeyDown(interactionKey))
            {
                PickupItem();
            }
        }
    }

    private void CheckPlayerDistance()
    {
        if (playerCamera != null)
        {
            float distance = Vector3.Distance(transform.position, playerCamera.transform.position);
            isInRange = distance <= interactionDistance;
        }
    }

    private void ShowInteractionPrompt()
    {
        // Esta es una implementaci�n b�sica. Puedes mejorarla con un sistema de UI
        // Opci�n 1: Dibuja el texto en pantalla con GUI
        GUI.color = Color.white;
        GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height / 2 + 50, 200, 30), interactionPrompt);

        // Opci�n 2: Mostrar un canvas con texto sobre el objeto (no implementado aqu�)
    }

    private void PickupItem()
    {
        // Buscar el sistema de inventario
        InventorySystem inventory = FindObjectOfType<InventorySystem>();

        if (inventory != null)
        {
            // Verificar si hay espacio en el inventario
            if (!inventory.IsInventoryFull())
            {
                // A�adir el �tem al inventario
                inventory.AddItem(itemId, itemName, itemIcon, itemDescription, itemType, quantity);

                // Destruir el objeto del mundo
                Destroy(gameObject);
            }
            else
            {
                Debug.Log("Inventario lleno, no se puede recoger: " + itemName);
                // Aqu� podr�as mostrar un mensaje al jugador
            }
        }
    }

    // Opcional: Dibuja un gizmo en el editor para visualizar el rango de interacci�n
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}