using UnityEngine;

public class CarController : MonoBehaviour
{
    public WheelCollider frontLeftCollider, frontRightCollider, rearLeftCollider, rearRightCollider;
    public Transform frontLeftMesh, frontRightMesh, rearLeftMesh, rearRightMesh;  // Visual wheels
    public float motorForce = 1500f;  // Acceleration
    public float steerAngle = 30f;    // Turning
    public float brakeForce = 3000f;  // Braking

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        float steer = Input.GetAxis("Horizontal") * steerAngle;
        float motor = Input.GetAxis("Vertical") * motorForce;
        float brake = Input.GetKey(KeyCode.Space) ? brakeForce : 0f;

        // Steering
        frontLeftCollider.steerAngle = steer;
        frontRightCollider.steerAngle = steer;

        // Motor (rear-wheel drive)
        rearLeftCollider.motorTorque = motor;
        rearRightCollider.motorTorque = motor;

        // Brakes
        frontLeftCollider.brakeTorque = brake;
        frontRightCollider.brakeTorque = brake;
        rearLeftCollider.brakeTorque = brake;
        rearRightCollider.brakeTorque = brake;

        // Rotate visual wheels
        UpdateWheelPose(frontLeftCollider, frontLeftMesh);
        UpdateWheelPose(frontRightCollider, frontRightMesh);
        UpdateWheelPose(rearLeftCollider, rearLeftMesh);
        UpdateWheelPose(rearRightCollider, rearRightMesh);
    }

    private void UpdateWheelPose(WheelCollider collider, Transform mesh)
    {
        if (mesh == null) return;
        Vector3 pos;
        Quaternion rot;
        collider.GetWorldPose(out pos, out rot);
        mesh.position = pos;
        mesh.rotation = rot;
    }
}