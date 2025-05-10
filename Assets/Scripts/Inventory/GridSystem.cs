using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSystem : MonoBehaviour
{
    // Tama�o de la cuadr�cula
    [SerializeField] private int width = 10;
    [SerializeField] private int height = 10;

    // Matriz para seguimiento de celdas ocupadas
    private InventoryItem[,] grid;

    // Propiedad para acceder al tama�o
    public int Width => width;
    public int Height => height;

    private void Awake()
    {
        // Inicializar la matriz de cuadr�cula
        grid = new InventoryItem[width, height];
    }

    private void Start()
    {
        // Inicializar la cuadr�cula
        ClearGrid();
    }

    // Limpiar toda la cuadr�cula
    public void ClearGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = null;
            }
        }
    }

    // Comprobar si un espacio est� ocupado
    public bool IsCellOccupied(Vector2Int position)
    {
        if (!IsPositionValid(position))
        {
            return true; // Posiciones inv�lidas se consideran ocupadas
        }

        return grid[position.x, position.y] != null;
    }

    // Comprobar si una posici�n est� dentro de los l�mites de la cuadr�cula
    public bool IsPositionValid(Vector2Int position)
    {
        return position.x >= 0 && position.x < width &&
               position.y >= 0 && position.y < height;
    }

    // Comprobar si un item puede ser colocado en una posici�n espec�fica
    public bool CanPlaceItem(InventoryItem item, Vector2Int position, bool rotated = false)
    {
        // Obtener el tama�o actual del item (considerando rotaci�n)
        Vector2Int size = rotated ? new Vector2Int(item.Size.y, item.Size.x) : item.Size;

        // Verificar si est� dentro de los l�mites
        if (position.x < 0 || position.y < 0 ||
            position.x + size.x > width || position.y + size.y > height)
        {
            return false;
        }

        // Verificar si todas las celdas necesarias est�n libres
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int checkPos = position + new Vector2Int(x, y);
                if (grid[checkPos.x, checkPos.y] != null && grid[checkPos.x, checkPos.y] != item)
                {
                    return false;
                }
            }
        }

        return true;
    }

    // Colocar un item en la cuadr�cula
    public bool PlaceItem(InventoryItem item, Vector2Int position, bool rotated = false)
    {
        if (!CanPlaceItem(item, position, rotated))
        {
            return false;
        }

        // Liberar celdas actuales del item (si ya est� en la cuadr�cula)
        RemoveItem(item);

        // Actualizar datos del item
        item.GridPosition = position;
        item.IsRotated = rotated;

        // Obtener tama�o considerando rotaci�n
        Vector2Int size = rotated ? new Vector2Int(item.Size.y, item.Size.x) : item.Size;

        // Ocupar celdas
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int cellPos = position + new Vector2Int(x, y);
                grid[cellPos.x, cellPos.y] = item;
            }
        }

        return true;
    }

    // Quitar un item de la cuadr�cula
    public void RemoveItem(InventoryItem item)
    {
        // Recorrer toda la cuadr�cula
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == item)
                {
                    grid[x, y] = null;
                }
            }
        }
    }

    // Intentar mover un item a una nueva posici�n
    public bool TryMoveItem(InventoryItem item, Vector2Int newPosition)
    {
        if (item == null) return false;

        // Verificar si se puede colocar en la nueva posici�n
        if (CanPlaceItem(item, newPosition, item.IsRotated))
        {
            PlaceItem(item, newPosition, item.IsRotated);
            return true;
        }

        return false;
    }

    // Rotar un item en su posici�n actual
    public bool TryRotateItem(InventoryItem item)
    {
        if (item == null || !item.IsRotatable) return false;

        // Verificar si puede ser rotado en su posici�n actual
        if (CanPlaceItem(item, item.GridPosition, !item.IsRotated))
        {
            // Quitar item de la cuadr�cula
            RemoveItem(item);

            // Rotar y colocar de nuevo
            item.Rotate();
            PlaceItem(item, item.GridPosition, item.IsRotated);

            return true;
        }

        return false;
    }

    // Obtener el item en una posici�n espec�fica
    public InventoryItem GetItemAt(Vector2Int position)
    {
        if (!IsPositionValid(position))
        {
            return null;
        }

        return grid[position.x, position.y];
    }

    // Encontrar un espacio disponible para un item
    public Vector2Int? FindAvailableSpace(InventoryItem item)
    {
        // Intentar encontrar espacio con orientaci�n normal
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (CanPlaceItem(item, pos, false))
                {
                    return pos;
                }
            }
        }

        // Si el item es rotable, intentar con orientaci�n rotada
        if (item.IsRotatable)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (CanPlaceItem(item, pos, true))
                    {
                        return pos;
                    }
                }
            }
        }

        // No se encontr� espacio disponible
        return null;
    }
}