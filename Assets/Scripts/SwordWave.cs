using UnityEngine;

public class SwordWave : MonoBehaviour
{
    public float speed = 12f;
    public int damage = 1;
    public float lifeTime = 0.8f;

    private System.Collections.Generic.HashSet<int> hitEnemies = new System.Collections.Generic.HashSet<int>();

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(Vector3.up * speed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null) enemy = other.GetComponentInParent<Enemy>();

        if (enemy != null)
        {
            int id = enemy.gameObject.GetInstanceID();
            if (!hitEnemies.Contains(id))
            {
                enemy.TakeDamage(damage);
                hitEnemies.Add(id);
            }
        }
    }
}
