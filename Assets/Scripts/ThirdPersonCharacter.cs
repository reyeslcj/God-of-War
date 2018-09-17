using UnityEngine;
using Cinemachine;

namespace CJ.GodOfWar
{
    [RequireComponent(typeof(Rigidbody))]
	[RequireComponent(typeof(CapsuleCollider))]
	[RequireComponent(typeof(Animator))]
	public class ThirdPersonCharacter : MonoBehaviour
	{
        static readonly int ThrowAxeHash = Animator.StringToHash("Throw");
        static readonly int AimHash = Animator.StringToHash("Aim");
        static readonly int AxeThrownHash = Animator.StringToHash("AxeThrown");
        static readonly int AxeRecallingHash = Animator.StringToHash("AxeRecalling");
        
        [SerializeField] float m_MovingTurnSpeed = 360;
		[SerializeField] float m_StationaryTurnSpeed = 180;
		[SerializeField] float m_JumpPower = 12f;
		[Range(1f, 4f)][SerializeField] float m_GravityMultiplier = 2f;
		[SerializeField] float m_RunCycleLegOffset = 0.2f; //specific to the character in sample assets, will need to be modified to work with others
		[SerializeField] float m_MoveSpeedMultiplier = 1f;
		[SerializeField] float m_AnimSpeedMultiplier = 1f;
		[SerializeField] float m_GroundCheckDistance = 0.1f;
        [SerializeField] LayerMask m_CrouchCheckLayer;

		Rigidbody m_Rigidbody;
		Animator m_Animator;
		bool m_IsGrounded;
		float m_OrigGroundCheckDistance;
		const float k_Half = 0.5f;
		float m_TurnAmount;
		float m_ForwardAmount;
		Vector3 m_GroundNormal;
		float m_CapsuleHeight;
		Vector3 m_CapsuleCenter;
		CapsuleCollider m_Capsule;
		bool m_Crouching;

        [SerializeField] Canvas m_CrossHair;
        [SerializeField] CinemachineFreeLook m_FreeLookCamera;
        [SerializeField] float m_ZoomSpeed;
        [SerializeField] float m_ZoomFOV;
        [SerializeField] LayerMask m_HitMask;
        [SerializeField] Transform m_WeaponSocket; 
        public Transform WeaponSocket { get { return m_WeaponSocket; } }
        [SerializeField] Axe axe;
        [SerializeField] RandomAudio m_AxeReturnedAudio;
        [SerializeField] RandomAudio m_RingAudio;

        Transform m_MainCam;
        bool m_AxeThrown;
        bool m_Aiming;
        bool m_AxeRecalling;
        public bool AxeRecalling { get { return m_AxeRecalling; } }
        Vector3 m_AimOffset;
        Vector3 m_AimDirection;
        Vector3 m_ThrowStartPos;
        Vector3 m_ThrowStartDir;

        void Start()
		{
			m_Animator = GetComponent<Animator>();
			m_Rigidbody = GetComponent<Rigidbody>();
			m_Capsule = GetComponent<CapsuleCollider>();
            m_MainCam = Camera.main.transform;
			m_CapsuleHeight = m_Capsule.height;
			m_CapsuleCenter = m_Capsule.center;

			m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
			m_OrigGroundCheckDistance = m_GroundCheckDistance;
		}


		public void Move(Vector3 move, bool crouch, bool jump)
		{
            if (m_Aiming || m_AxeRecalling)
                move *= 0.5f;

			// convert the world relative moveInput vector into a local-relative
			// turn amount and forward amount required to head in the desired
			// direction.
			if (move.magnitude > 1f) move.Normalize();
			move = transform.InverseTransformDirection(move);
			CheckGroundStatus();
			move = Vector3.ProjectOnPlane(move, m_GroundNormal);
			m_TurnAmount = Mathf.Atan2(move.x, move.z);
			m_ForwardAmount = move.z;

			ApplyExtraTurnRotation();

			// control and velocity handling is different when grounded and airborne:
			if (m_IsGrounded)
			{
				HandleGroundedMovement(crouch, jump);
			}
			else
			{
				HandleAirborneMovement();
			}

			ScaleCapsuleForCrouching(crouch);
			PreventStandingInLowHeadroom();

			// send input and other state parameters to the animator
			UpdateAnimator(move);
		}


		void ScaleCapsuleForCrouching(bool crouch)
		{
			if (m_IsGrounded && crouch)
			{
				if (m_Crouching) return;
				m_Capsule.height = m_Capsule.height / 2f;
				m_Capsule.center = m_Capsule.center / 2f;
				m_Crouching = true;
			}
			else
			{
				Ray crouchRay = new Ray(m_Rigidbody.position + Vector3.up * m_Capsule.radius * k_Half, Vector3.up);
				float crouchRayLength = m_CapsuleHeight - m_Capsule.radius * k_Half;
				if (Physics.SphereCast(crouchRay, m_Capsule.radius * k_Half, crouchRayLength, m_CrouchCheckLayer, QueryTriggerInteraction.Ignore))
				{
					m_Crouching = true;
					return;
				}
				m_Capsule.height = m_CapsuleHeight;
				m_Capsule.center = m_CapsuleCenter;
				m_Crouching = false;
			}
		}

		void PreventStandingInLowHeadroom()
		{
			// prevent standing up in crouch-only zones
			if (!m_Crouching)
			{
				Ray crouchRay = new Ray(m_Rigidbody.position + Vector3.up * m_Capsule.radius * k_Half, Vector3.up);
				float crouchRayLength = m_CapsuleHeight - m_Capsule.radius * k_Half;
				if (Physics.SphereCast(crouchRay, m_Capsule.radius * k_Half, crouchRayLength, m_CrouchCheckLayer, QueryTriggerInteraction.Ignore))
				{
					m_Crouching = true;
 				}
			}
		}


		void UpdateAnimator(Vector3 move)
		{
			// update the animator parameters
			m_Animator.SetFloat("Forward", m_ForwardAmount, 0.1f, Time.deltaTime);
			m_Animator.SetFloat("Turn", m_TurnAmount, 0.1f, Time.deltaTime);
			m_Animator.SetBool("Crouch", m_Crouching);
			m_Animator.SetBool("OnGround", m_IsGrounded);
			if (!m_IsGrounded)
			{
				m_Animator.SetFloat("Jump", m_Rigidbody.velocity.y);
			}

			// calculate which leg is behind, so as to leave that leg trailing in the jump animation
			// (This code is reliant on the specific run cycle offset in our animations,
			// and assumes one leg passes the other at the normalized clip times of 0.0 and 0.5)
			float runCycle =
				Mathf.Repeat(
					m_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime + m_RunCycleLegOffset, 1);
			float jumpLeg = (runCycle < k_Half ? 1 : -1) * m_ForwardAmount;
			if (m_IsGrounded)
			{
				m_Animator.SetFloat("JumpLeg", jumpLeg);
			}

			// the anim speed multiplier allows the overall speed of walking/running to be tweaked in the inspector,
			// which affects the movement speed because of the root motion.
			if (m_IsGrounded && move.magnitude > 0)
			{
				m_Animator.speed = m_AnimSpeedMultiplier;
			}
			else
			{
				// don't use that while airborne
				m_Animator.speed = 1;
			}
		}


		void HandleAirborneMovement()
		{
			// apply extra gravity from multiplier:
			Vector3 extraGravityForce = (Physics.gravity * m_GravityMultiplier) - Physics.gravity;
			m_Rigidbody.AddForce(extraGravityForce);

			m_GroundCheckDistance = m_Rigidbody.velocity.y < 0 ? m_OrigGroundCheckDistance : 0.01f;
		}


		void HandleGroundedMovement(bool crouch, bool jump)
		{
			// check whether conditions are right to allow a jump:
			if (jump && !crouch && m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Grounded"))
			{
				// jump!
				m_Rigidbody.velocity = new Vector3(m_Rigidbody.velocity.x, m_JumpPower, m_Rigidbody.velocity.z);
				m_IsGrounded = false;
				m_Animator.applyRootMotion = false;
				m_GroundCheckDistance = 0.1f;
			}
		}

		void ApplyExtraTurnRotation()
		{
			// help the character turn faster (this is in addition to root rotation in the animation)
			float turnSpeed = Mathf.Lerp(m_StationaryTurnSpeed, m_MovingTurnSpeed, m_ForwardAmount);
			transform.Rotate(0, m_TurnAmount * turnSpeed * Time.deltaTime, 0);
		}


		public void OnAnimatorMove()
		{
			// we implement this function to override the default root motion.
			// this allows us to modify the positional speed before it's applied.
			if (m_IsGrounded && Time.deltaTime > 0)
			{
				Vector3 v = (m_Animator.deltaPosition * m_MoveSpeedMultiplier) / Time.deltaTime;

				// we preserve the existing y part of the current velocity.
				v.y = m_Rigidbody.velocity.y;
				m_Rigidbody.velocity = v;
			}
		}


		void CheckGroundStatus()
		{
			RaycastHit hitInfo;
#if UNITY_EDITOR
			// helper to visualise the ground check ray in the scene view
			Debug.DrawLine(transform.position + (Vector3.up * 0.1f), transform.position + (Vector3.up * 0.1f) + (Vector3.down * m_GroundCheckDistance));
#endif
			// 0.1f is a small offset to start the ray from inside the character
			// it is also good to note that the transform position in the sample assets is at the base of the character
			if (Physics.Raycast(transform.position + (Vector3.up * 0.1f), Vector3.down, out hitInfo, m_GroundCheckDistance))
			{
				m_GroundNormal = hitInfo.normal;
				m_IsGrounded = true;
				m_Animator.applyRootMotion = true;
			}
			else
			{
				m_IsGrounded = false;
				m_GroundNormal = Vector3.up;
				m_Animator.applyRootMotion = false;
			}
		}

        public void HandleAim(bool aim)
        {
            if (aim)
                Aim();
            else
                ReleaseAim();
        }

        void Aim()
        {
            //Update Camera
            if (!m_AxeThrown)
            {
                m_Aiming = true;
                m_CrossHair.enabled = true;
                m_FreeLookCamera.m_Lens.FieldOfView = Mathf.Lerp(m_FreeLookCamera.m_Lens.FieldOfView, m_ZoomFOV, Time.deltaTime * m_ZoomSpeed);
                Vector3 lookDir = Vector3.Scale(m_MainCam.forward, new Vector3(1, 0, 1)).normalized;
                transform.rotation = Quaternion.LookRotation(lookDir);
                m_Animator.SetBool(AimHash, true);
            }
        }

        void ReleaseAim()
        {
            m_Aiming = false;
            m_CrossHair.enabled = false;
            m_FreeLookCamera.m_Lens.FieldOfView = Mathf.Lerp(m_FreeLookCamera.m_Lens.FieldOfView, 40.0f, Time.deltaTime * m_ZoomSpeed);
            m_Animator.SetBool(AimHash, false);
        }

        public void HandleThrow(bool startThrow)
        {
            if (startThrow && m_Aiming && !m_AxeThrown)
            {
                m_Animator.SetBool(ThrowAxeHash, true);
                m_ThrowStartPos = m_MainCam.position;
                m_ThrowStartDir = m_MainCam.forward;
            }
        }

        void Throw()
        {
            RaycastHit hit;
      
            if (Physics.Raycast(m_ThrowStartPos, m_ThrowStartDir, out hit, 1000.0f, m_HitMask, QueryTriggerInteraction.Ignore))
            {
                m_AimDirection = (hit.point - m_WeaponSocket.position).normalized;
            }
            else
            {
                Vector3 endPos = m_ThrowStartPos + m_ThrowStartDir * 1000.0f;
                m_AimDirection = (endPos - m_WeaponSocket.position).normalized;
            }
            Debug.DrawRay(m_WeaponSocket.position, m_AimDirection * 1000f, Color.green, 10.0f);
            axe.Throw(m_AimDirection, this);
            m_AxeThrown = true;
            m_Animator.SetBool(AxeThrownHash, m_AxeThrown);
            m_Animator.SetBool(ThrowAxeHash, false);
        }

        public void AxeReturned()
        {
            axe.transform.SetParent(m_WeaponSocket);
            axe.transform.localPosition = Vector3.zero;
            axe.transform.localRotation = Quaternion.identity;
   
            m_AxeThrown = false;
            m_AxeRecalling = false;
            m_Animator.SetBool(AxeThrownHash, m_AxeThrown);
            m_Animator.SetBool(AxeRecallingHash, m_AxeRecalling);
        }

        public void HandleRecall(bool recall)
        {
            if (recall)
                Recall();
        }

        void Recall()
        {
            if(m_AxeThrown && !m_AxeRecalling)
            {
                m_AxeRecalling = true;
                m_Animator.SetBool(AxeRecallingHash, m_AxeRecalling);
                axe.Return();
            }
        }

        void PlayAxeReturned()
        {
            m_AxeReturnedAudio.Play();
            m_RingAudio.Play();
        }
	}
}
