using UnityEngine;

namespace GenshinImpactMovementSystem
{
    public class PlayerUnarmedAttack : MonoBehaviour
    {
        [SerializeField] private Transform attackPoint;
        [SerializeField] private float radius = 0.55f;
        [SerializeField] private LayerMask hittableLayers;
        [SerializeField] private int damage = 10;

        private readonly Collider[] hitBuffer = new Collider[16];

        [ContextMenu("Test Perform Attack Hit")]
        public void PerformAttackHit()
        {
            Debug.Log("PerformAttackHit CALLED", this);

            if (attackPoint == null)
            {
                Debug.LogWarning("AttackPoint is not assigned.", this);
                return;
            }

            int hitCount = Physics.OverlapSphereNonAlloc(
                attackPoint.position,
                radius,
                hitBuffer,
                hittableLayers,
                QueryTriggerInteraction.Ignore
            );

            Debug.Log($"OverlapSphere hitCount = {hitCount}", this);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = hitBuffer[i];

                if (hitCollider == null)
                {
                    continue;
                }

                Debug.Log($"Hit collider: {hitCollider.name}", hitCollider);

                if (hitCollider.TryGetComponent(out IDamageable damageable))
                {
                    damageable.TakeDamage(damage);
                    Debug.Log($"Damage applied: {damage}", hitCollider);
                }
                else
                {
                    Debug.LogWarning($"No IDamageable on {hitCollider.name}", hitCollider);
                }

                hitBuffer[i] = null;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (attackPoint == null)
            {
                return;
            }

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, radius);
        }
    }

    public interface IDamageable
    {
        void TakeDamage(int damage);
    }
}