using UnityEngine;
using UnityEngine.Events;

namespace CJ.GodOfWar.DamageSystem
{
    public partial class Damageable : MonoBehaviour
    {

        public int maxHitPoints;

        public int currentHitPoints { get; private set; }

        public UnityEvent OnDeath, OnReceiveDamage;
 
        protected Collider m_Collider;

        void Start()
        {
            ResetDamage();
            m_Collider = GetComponent<Collider>();
        }

        public void ResetDamage()
        {
            currentHitPoints = maxHitPoints;
        }

        public void SetColliderState(bool enabled)
        {
            m_Collider.enabled = enabled;
        }

        public void ApplyDamage(DamageMessage data)
        {
            if (currentHitPoints <= 0)
            {
                return;
            }

            currentHitPoints -= data.amount;

            if (currentHitPoints <= 0)
                OnDeath.Invoke();
            else
                OnReceiveDamage.Invoke();
        }
    }
}