using UnityEngine;

namespace CJ.GodOfWar
{
    public class AxeReturnSMB : StateMachineBehaviour
    {
        static readonly int ReturnSpeedHash = Animator.StringToHash("ReturnSpeed");

        public override void OnStateEnter(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex)
        {
            Axe axe = animator.GetComponentInParent<Axe>();
            if(axe)
            {
                float newSpeed = 1.0f / axe.distance;
                animator.SetFloat(ReturnSpeedHash, newSpeed);
            }
        }
    }
}
