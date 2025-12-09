using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;  // Drag Player Car here
    public Vector3 offset = new Vector3(0, 2.5f, -5f);  // Behind/up
    public float smoothSpeed = 0.125f;  // Follow smoothness
    public float minDist = 3f, maxDist = 10f;  // Zoom limits
    public float speedMultiplier = 0.5f;  // Distance on speed

    private Rigidbody targetRb;

    void Start()
    {
        if (target) targetRb = target.GetComponent<Rigidbody>();
    }

    void LateUpdate()
    {
        if (!target) return;

        // Speed-based distance
        float speed = targetRb ? targetRb.linearVelocity.magnitude : 0f;
        float dist = Mathf.Lerp(minDist, maxDist, speed * speedMultiplier);
        Vector3 desiredPos = target.position + target.rotation * new Vector3(offset.x, offset.y, -dist);

        // Smooth follow
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed);
        transform.LookAt(target.position + target.forward * 2f);  // Look ahead slightly
    }
}