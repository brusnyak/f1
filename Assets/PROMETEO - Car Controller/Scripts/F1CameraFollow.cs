using UnityEngine;

public class F1CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // The F1 car to follow

    [Header("Camera Position")]
    [Range(2f, 10f)]
    public float distance = 6f; // Distance behind car
    [Range(1f, 5f)]
    public float height = 2.5f; // Height above car
    [Range(0f, 3f)]
    public float heightDamping = 2f; // How smooth the height follows
    [Range(0f, 5f)]
    public float rotationDamping = 3f; // How smooth the rotation follows

    [Header("Camera Angle")]
    [Range(-20f, 20f)]
    public float lookDownAngle = 10f; // Angle looking down at car

    [Header("Speed-Based Adjustments")]
    public bool useSpeedBasedFOV = true;
    [Range(50f, 80f)]
    public float baseFOV = 60f;
    [Range(0f, 20f)]
    public float speedFOVMultiplier = 5f; // Extra FOV at high speeds
    [Range(0f, 3f)]
    public float speedDistanceReduction = 1.5f; // How much distance is reduced at max speed

    private Camera cam;
    private PrometeoCarController carController;

    void Start()
    {
        cam = GetComponent<Camera>();
        
        if(target != null)
        {
            carController = target.GetComponent<PrometeoCarController>();
        }
        
        if(cam != null)
        {
            cam.fieldOfView = baseFOV;
        }
    }

    void LateUpdate()
    {
        if(target == null) return;

        // Calculate speed-based distance reduction
        float currentSpeed = (carController != null) ? Mathf.Abs(carController.carSpeed) : 0f;
        float speedRatio = currentSpeed / ((carController != null) ? carController.maxForwardSpeed : 320f);
        float dynamicDistance = distance - (speedDistanceReduction * speedRatio);
        dynamicDistance = Mathf.Max(dynamicDistance, distance - speedDistanceReduction); // Ensure it doesn't go too close

        // Since car model faces backward, we need to position camera in FRONT of transform.forward
        // (which is visually behind the car)
        Vector3 wantedPosition = target.position + (target.forward * dynamicDistance) + (Vector3.up * height);
        
        // Smoothly move to desired position
        transform.position = Vector3.Lerp(transform.position, wantedPosition, heightDamping * Time.deltaTime);

        // Make camera look in opposite direction (180° flip) to see the car
        Vector3 lookDirection = -target.forward; // Flip 180°
        Quaternion wantedRotation = Quaternion.LookRotation(lookDirection);
        
        // Apply downward angle to look at the car
        wantedRotation *= Quaternion.Euler(lookDownAngle, 0, 0);
        
        // Smoothly rotate to desired rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, wantedRotation, rotationDamping * Time.deltaTime);

        // Speed-based FOV (widens at high speeds for dramatic effect)
        if(useSpeedBasedFOV && carController != null && cam != null)
        {
            float speed = Mathf.Abs(carController.carSpeed);
            float targetFOV = baseFOV + (speed / 320f) * speedFOVMultiplier;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * 2f);
        }
    }

    // Helper to reset camera behind car (useful after respawn)
    public void ResetCamera()
    {
        if(target == null) return;
        
        // Calculate speed-based distance reduction for reset (assume max speed for consistent reset)
        float resetDynamicDistance = distance - speedDistanceReduction;
        resetDynamicDistance = Mathf.Max(resetDynamicDistance, distance - speedDistanceReduction);

        Vector3 resetPosition = target.position + (target.forward * resetDynamicDistance) + (Vector3.up * height);
        transform.position = resetPosition;
        
        // Look opposite direction (180° flip)
        transform.rotation = Quaternion.LookRotation(-target.forward) * Quaternion.Euler(lookDownAngle, 0, 0);
    }

    // Debug visualization in Scene view
    void OnDrawGizmosSelected()
    {
        if(target == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, target.position);
        
        Gizmos.color = Color.green;
        Vector3 wantedPosition = target.position + (target.forward * distance) + (Vector3.up * height);
        Gizmos.DrawWireSphere(wantedPosition, 0.5f);
    }
}
