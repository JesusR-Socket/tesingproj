using UnityEngine;

namespace GenshinImpactMovementSystem
{
    public class DummyDamageable : MonoBehaviour, IDamageable
    {
        [SerializeField] private int health = 100;

        public void TakeDamage(int damage)
        {
            health -= damage;
            Debug.Log($"{name} took {damage} damage. HP left: {health}");

            if (health <= 0)
            {
                Debug.Log($"{name} died.");
            }
        }
    }
}
