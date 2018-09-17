using UnityEngine;
using System.Collections;
using CJ.GodOfWar.DamageSystem;

namespace CJ.GodOfWar
{
    public class Axe : MonoBehaviour
    {
        static readonly int ThrownHash = Animator.StringToHash("Thrown");
        static readonly int WallHash = Animator.StringToHash("Wall");
        static readonly int FloorHash = Animator.StringToHash("Floor");
        static readonly int WiggleHash = Animator.StringToHash("Wiggle");
        static readonly int ReturnHash = Animator.StringToHash("Return");
        static readonly int InHandHash = Animator.StringToHash("InHand");
        
        public float throwForce;
        [HideInInspector]
        public float distance;

        [SerializeField] Transform m_ContactPoint;
        [SerializeField] Collider m_Collider;
        [SerializeField] LayerMask m_AxeReturnTriggerSoundMask;
        [SerializeField] float m_IdealDistance;
        [SerializeField] float m_WiggleTime = 0.3f;
        [SerializeField] float m_ReturnTime = 1.0f;
        [SerializeField] AnimationCurve m_CurveAxeReturn;
        [SerializeField] AnimationCurve m_CurveAxeReturnRight;
        [SerializeField] AnimationCurve m_CurveAxeRotation;

        Vector3 m_Direction;
        Vector3 m_CollisionPoint;
        Vector3 m_CacheVelo;
        Vector3 m_Initial;
        Vector3 m_PathRightVector;
        Vector3 m_Target;
        ThirdPersonCharacter m_Thrower;
        bool m_Thrown;
        bool m_Floor;
        bool m_Wall;
        bool m_InHand;
        bool m_Return;
        bool m_Landed;
        bool m_CanSound = true;
        bool m_Wiggle;
        Rigidbody m_RigidBody;
       
        Quaternion m_InitialRot;
        Animator m_Animator;
        AxeAudio m_AxeAudio;
        TrailRenderer m_AxeTrail;
        Quaternion m_ThrowRot;
        ColliderHit m_hit;

        void Awake()
        {
            m_RigidBody = GetComponent<Rigidbody>();
            m_Animator = GetComponentInChildren<Animator>();
            m_AxeAudio = GetComponentInChildren<AxeAudio>();
            m_AxeTrail = GetComponentInChildren<TrailRenderer>();
        }

        void Start()
        {
            m_AxeTrail.emitting = false;
            m_InHand = true;
        }

        void Update()
        {
            UpdateTargetTransform();
            UpdateRumble();   
            
            if(m_Thrown && !m_Landed)
            {
                m_CacheVelo = m_RigidBody.velocity;
            }
        }

        private void LateUpdate()
        {
            if (m_Landed)
            {
               Vector3 relativePos = m_ContactPoint.position - m_CollisionPoint;
               transform.position -= relativePos;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!m_Return)
            {
                ProcessHit(collision);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TryDealDamage(other);
            TryPlayAxeReturnSFX(other);
        }

        void TryDealDamage(Collider other)
        {
            if (m_InHand)
                return;

            Damageable damageable = other.GetComponent<Damageable>();
            if (damageable)
            {
                Damageable.DamageMessage damager = new Damageable.DamageMessage { damager = this, amount = 1 };
                damageable.ApplyDamage(damager);
            }
        }

        void TryPlayAxeReturnSFX(Collider other)
        {
            if (m_CanSound && m_Return && m_AxeReturnTriggerSoundMask == (m_AxeReturnTriggerSoundMask | (1 << other.gameObject.layer)))
            {
                m_CanSound = false;

                AudioData data = new AudioData { pitch = 0.35f, volume = 0.75f };
                
                m_AxeAudio.axeHit.Play(data);

                Invoke("CanSound", 0.02f);
            }
        }

        public void Throw(Vector3 direction, ThirdPersonCharacter thrower)
        {
            m_Direction = direction;
            m_Thrower = thrower;
            m_Thrown = true;
            m_Floor = false;
            m_Wall = false;
            m_InHand = false;

            m_Collider.isTrigger = false;
            m_RigidBody.isKinematic = false;
            m_RigidBody.useGravity = true;
            m_RigidBody.velocity = m_Direction * throwForce;
        
            Straighten();
            transform.SetParent(null, true);  
            m_AxeTrail.emitting = true;
            m_InitialRot = transform.rotation;
            m_AxeAudio.axeRumble.Play();
            UpdateAnimatorParam();
        }

        void Straighten()
        {
            m_ThrowRot = Quaternion.LookRotation(m_Direction);
            transform.rotation = m_ThrowRot;
        }

        void UpdateTargetTransform()
        {
            if (m_Return)
                m_Target = m_Thrower.WeaponSocket.position;
        }

        void UpdateRumble()
        {
            if(m_Thrown)
            {
                float percentage = m_RigidBody.velocity.sqrMagnitude / (throwForce * throwForce);
                m_AxeAudio.axeRumble.volume = Mathf.Clamp(percentage, 0.0f, 0.75f);
                m_AxeAudio.axeRumble.pitch = Mathf.Clamp(percentage, 0.0f, 2.0f);
            }
        }

        void ProcessHit(Collision collision)
        {  
            m_RigidBody.isKinematic = true;
            m_Thrown = false;
            m_CanSound = false;

            m_AxeAudio.axeRumble.Stop();
            m_AxeAudio.PlayRandomHit();

            float dot = Vector3.Dot(collision.contacts[0].normal, Vector3.up);
            m_Wall = Mathf.Abs(dot) <= 0.5f;
            m_Floor = dot > 0.5f;
            UpdateAnimatorParam();

            m_CollisionPoint = collision.contacts[0].point;
            Vector3 newDir = (Vector3.Scale(m_CacheVelo, new Vector3(1, 0, 1))).normalized;    
            Quaternion targetRot = Quaternion.LookRotation(newDir, Vector3.up);     
            transform.rotation = targetRot;
            Debug.DrawRay(transform.position, newDir * 10f, Color.red, 10f);

            m_Landed = true;
        }

        public void Return()
        {
            StartReturn();
        }

        void StartReturn()
        {
            m_Collider.isTrigger = true;
            m_RigidBody.isKinematic = true;

            if (m_Landed)
                m_Wiggle = true;
            else
                m_Thrown = true;
     
            UpdateAnimatorParam();

            m_Landed = false;
            m_Initial = transform.position;
            m_InitialRot = transform.rotation;

            Vector3 relativePos = m_Thrower.WeaponSocket.position - m_Initial;
            distance = Mathf.Clamp(relativePos.magnitude / m_IdealDistance, 0.5f, 3.0f);
            m_PathRightVector = Vector3.Cross(relativePos.normalized, Vector3.up);

            Invoke("CanSound", 0.02f);

            StartCoroutine(StartReturnRoutine());
        }

        IEnumerator StartReturnRoutine()
        {
            if (m_Wiggle)
            {
                yield return new WaitForSeconds(m_WiggleTime);
                m_Wiggle = false;
            }

            m_Return = true;
            UpdateAnimatorParam();
            m_AxeAudio.axeRumble.Play();

            float newRate = 1.0f / distance;
           
            float currentTime = 0f;

            CalculateReturnTransform(currentTime);

            while (currentTime < m_ReturnTime)
            {
                currentTime += Time.deltaTime * newRate;
                CalculateReturnTransform(currentTime);
                yield return null;
            }

            FinishedReturn();
            m_AxeAudio.axeRumble.Stop();
        }

        void CalculateReturnTransform(float curveTime)
        {
            float axeReturnValue = m_CurveAxeReturn.Evaluate(curveTime);
            float axeRotValue = m_CurveAxeRotation.Evaluate(curveTime);
            Vector3 targetPos = m_Initial + ((m_Target - m_Initial) * axeReturnValue) + (m_PathRightVector * m_CurveAxeReturnRight.Evaluate(curveTime) * 3.0f * distance);
            Quaternion deltaRot = (Quaternion.Inverse(m_InitialRot) * m_Thrower.WeaponSocket.rotation).normalized;
            Quaternion targetRot = m_InitialRot * (Quaternion.Lerp(Quaternion.identity,deltaRot, axeRotValue));
            transform.SetPositionAndRotation(targetPos, targetRot);
            m_AxeAudio.axeRumble.pitch = axeReturnValue;
        }

        void FinishedReturn()
        {
            m_Thrower.AxeReturned();

            m_Return = false;
            m_Thrown = false;
            m_InHand = true;
            m_Wall = false;
            m_Floor = false;

            UpdateAnimatorParam();

            m_AxeTrail.emitting = false;
        }

        void UpdateAnimatorParam()
        {
            m_Animator.SetBool(ThrownHash, m_Thrown);
            m_Animator.SetBool(WallHash, m_Wall);
            m_Animator.SetBool(FloorHash, m_Floor);
            m_Animator.SetBool(WiggleHash, m_Wiggle);
            m_Animator.SetBool(ReturnHash, m_Return);
            m_Animator.SetBool(InHandHash, m_InHand);
        }
            
        void CanSound()
        {
            m_CanSound = true;
        }
    }
}