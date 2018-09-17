using UnityEngine;

public class ColliderHit : MonoBehaviour {
    public System.Action<Collision> OnHit;

    private void OnCollisionEnter(Collision collision)
    {
        if (OnHit != null)
            OnHit(collision);
    }
}
