using UnityEngine;

namespace CJ.GodOfWar.DamageSystem
{
    public partial class Damageable : MonoBehaviour
    {
        public struct DamageMessage
        {
            public MonoBehaviour damager;
            public int amount;
        }
    } 
}
