// Bullet.cs
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private float speed;
    private float damage;
    private float lifetime;
    private Vector3 direction;
    private bool isInitialized = false;

    public void Initialize(Vector3 direction, float speed, float damage, float lifetime)
    {
        this.direction = direction;
        this.speed = speed;
        this.damage = damage;
        this.lifetime = lifetime;
        isInitialized = true;

        // Destruir bala después del tiempo de vida
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (!isInitialized) return;

        // Mover bala
        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Comprobar si colisiona con un enemigo u objeto dañable
        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }

        // Crear efecto de impacto aquí si lo deseas

        // Destruir la bala
        Destroy(gameObject);
    }
}

// Interfaz para objetos que pueden recibir daño
public interface IDamageable
{
    void TakeDamage(float damage);
}