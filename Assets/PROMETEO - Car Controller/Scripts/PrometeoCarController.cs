/*
MESSAGE FROM CREATOR: This script was coded by Mena. Modified for F1 Racing.
OPTIMIZED: Fixed speed limits bug and simplified code structure.
*/

using System;
using UnityEngine;
using UnityEngine.UI;

public class PrometeoCarController : MonoBehaviour
{
    //CAR SETUP
    [Space(20)]
    [Header("F1 PERFORMANCE")]
    [Space(10)]
    [Range(20, 400)]
    public int maxBackwardSpeed = 45;
    [Range(10, 400)]
    public int maxForwardSpeed = 320;
    [Range(1, 20)]
    public int accelerationMultiplier = 10;
    [Space(10)]
    [Range(10, 45)]
    public int maxSteeringAngle = 18;
    [Range(0.1f, 1f)]
    public float steeringSpeed = 0.8f;
    [Space(10)]
    [Range(100, 3000)]
    public int brakeForce = 1800;
    [Range(1, 10)]
    public int decelerationMultiplier = 4;

    //AERODYNAMICS & TRACTION
    [Space(20)]
    [Header("F1 PHYSICS SYSTEMS")]
    [Space(10)]
    public bool useDownforce = true;
    [Range(0.5f, 5f)]
    public float downforceCoefficient = 4f;
    [Space(10)]
    [Range(1f, 5f)]
    public float antiRollForceMultiplier = 3f;
    [Space(10)]
    public bool useTractionControl = true;
    [Range(0f, 1f)]
    public float tractionControlStrength = 0.7f;
    [Space(10)]
    public Vector3 bodyMassCenter;

    // Power-up modifiers
    [HideInInspector] public float currentMaxForwardSpeed;
    [HideInInspector] public float currentAccelerationMultiplier;
    [HideInInspector] public float currentDownforceCoefficient;
    [HideInInspector] public float currentAntiRollForceMultiplier;
    [HideInInspector] public bool collisionReduced = false;

    //WHEELS
    [Space(20)]
    [Header("WHEELS")]
    [Space(10)]
    public GameObject frontLeftMesh;
    public WheelCollider frontLeftCollider;
    [Space(10)]
    public GameObject frontRightMesh;
    public WheelCollider frontRightCollider;
    [Space(10)]
    public GameObject rearLeftMesh;
    public WheelCollider rearLeftCollider;
    [Space(10)]
    public GameObject rearRightMesh;
    public WheelCollider rearRightCollider;

    //EFFECTS
    [Space(20)]
    [Header("EFFECTS")]
    [Space(10)]
    public bool useEffects = false;
    public ParticleSystem RLWParticleSystem;
    public ParticleSystem RRWParticleSystem;
    [Space(10)]
    public TrailRenderer RLWTireSkid;
    public TrailRenderer RRWTireSkid;
    [Space(10)]
    [Range(0.3f, 1f)]
    public float brakingEffectThreshold = 0.6f;
    [Space(10)]
    [Range(50f, 500f)]
    public float skidSlipThreshold = 150f;
    [HideInInspector]
    public bool isSkidding;

    //UI
    [Space(20)]
    [Header("UI")]
    [Space(10)]
    public bool useUI = false;
    public Text carSpeedText;

    //SOUNDS
    [Space(20)]
    [Header("SOUNDS")]
    [Space(10)]
    public bool useSounds = false;
    public AudioSource carEngineSound;
    public AudioSource tireScreechSound;
    float initialCarEngineSoundPitch;

    //CONTROLS
    [Space(20)]
    [Header("CONTROLS")]
    [Space(10)]
    public inputManager carInputManager;

    //CAR DATA
    [HideInInspector]
    public float carSpeed;
    [HideInInspector]
    public bool isHardBraking;

    //PRIVATE VARIABLES
    Rigidbody carRigidbody;
    float steeringAxis;
    float throttleAxis;
    float localVelocityZ;
    float localVelocityX;
    bool deceleratingCar;

    void Start()
    {
        carRigidbody = gameObject.GetComponent<Rigidbody>();
        carRigidbody.centerOfMass = bodyMassCenter;

        // Initialize current values
        currentMaxForwardSpeed = maxForwardSpeed;
        currentAccelerationMultiplier = accelerationMultiplier;
        currentDownforceCoefficient = downforceCoefficient;
        currentAntiRollForceMultiplier = antiRollForceMultiplier;

        this.enabled = true;

        if (carEngineSound != null)
        {
            initialCarEngineSoundPitch = carEngineSound.pitch;
        }

        if (useUI)
        {
            InvokeRepeating("CarSpeedUI", 0f, 0.1f);
        }

        if (useSounds)
        {
            InvokeRepeating("CarSounds", 0f, 0.1f);
        }
        else
        {
            if (carEngineSound != null) carEngineSound.Stop();
            if (tireScreechSound != null) tireScreechSound.Stop();
        }

        if (!useEffects)
        {
            if (RLWParticleSystem != null) RLWParticleSystem.Stop();
            if (RRWParticleSystem != null) RRWParticleSystem.Stop();
            if (RLWTireSkid != null) RLWTireSkid.emitting = false;
            if (RRWTireSkid != null) RRWTireSkid.emitting = false;
        }

        carInputManager = GetComponent<inputManager>();
        if (carInputManager == null)
        {
            Debug.LogError("InputManager component not found on the car. Please add it.");
        }
    }

    void Update()
    {
        // Calculate car speed
        float targetSpeed = carRigidbody.linearVelocity.magnitude * 3.6f;
        carSpeed = Mathf.Lerp(carSpeed, targetSpeed, Time.deltaTime * 5f);

        // Local velocity
        localVelocityX = transform.InverseTransformDirection(carRigidbody.linearVelocity).x;
        localVelocityZ = transform.InverseTransformDirection(carRigidbody.linearVelocity).z;

        // CAR CONTROLS
        if (carInputManager != null)
        {
            SetInputs(carInputManager.vertical, carInputManager.horizontal, carInputManager.handbrake ? 1f : 0f);
        }

        // Apply physics systems
        if (useDownforce)
        {
            ApplyDownforce();
        }

        AnimateWheelMeshes();
    }

    void FixedUpdate()
    {
        if (useTractionControl)
        {
            ApplyTractionControl();
        }

        ApplyAntiRoll();
    }

    void ApplyAntiRoll()
    {
        float tiltAngle = Vector3.Dot(transform.right, Vector3.up);

        if (Mathf.Abs(tiltAngle) > 0.1f)
        {
            float speed = carRigidbody.linearVelocity.magnitude;
            float antiRollForce = -tiltAngle * 3000f * (1 + speed / 50f) * currentAntiRollForceMultiplier;
            carRigidbody.AddTorque(transform.forward * antiRollForce);
        }
    }

    //UI
    public void CarSpeedUI()
    {
        if (useUI && carSpeedText != null)
        {
            carSpeedText.text = Mathf.RoundToInt(Mathf.Abs(carSpeed)).ToString();
        }
    }

    //SOUNDS
    public void CarSounds()
    {
        if (useSounds)
        {
            if (carEngineSound != null)
            {
                float engineSoundPitch = initialCarEngineSoundPitch + (Mathf.Abs(carRigidbody.linearVelocity.magnitude) / 25f);
                carEngineSound.pitch = engineSoundPitch;
            }

            if (isHardBraking && Mathf.Abs(carSpeed) > 20f)
            {
                if (!tireScreechSound.isPlaying)
                {
                    tireScreechSound.Play();
                }
            }
            else if (tireScreechSound.isPlaying)
            {
                tireScreechSound.Stop();
            }
        }
    }

    void AnimateWheelMeshes()
    {
        Quaternion rot;
        Vector3 pos;

        frontLeftCollider.GetWorldPose(out pos, out rot);
        frontLeftMesh.transform.position = pos;
        frontLeftMesh.transform.rotation = rot;

        frontRightCollider.GetWorldPose(out pos, out rot);
        frontRightMesh.transform.position = pos;
        frontRightMesh.transform.rotation = rot;

        rearLeftCollider.GetWorldPose(out pos, out rot);
        rearLeftMesh.transform.position = pos;
        rearLeftMesh.transform.rotation = rot;

        rearRightCollider.GetWorldPose(out pos, out rot);
        rearRightMesh.transform.position = pos;
        rearRightMesh.transform.rotation = rot;
    }

    //F1 PHYSICS SYSTEMS
    void ApplyDownforce()
    {
        float speed = carRigidbody.linearVelocity.magnitude;
        float downforce = speed * speed * currentDownforceCoefficient;
        carRigidbody.AddForce(-transform.up * downforce);
    }

    void ApplyTractionControl()
    {
        WheelHit hit;
        bool rearLeftGrounded = rearLeftCollider.GetGroundHit(out hit);
        bool rearRightGrounded = rearRightCollider.GetGroundHit(out hit);

        if (rearLeftGrounded && rearRightGrounded)
        {
            float idealRPM = (carSpeed * 1000f) / (2 * Mathf.PI * rearLeftCollider.radius * 60f);

            float rearLeftSlip = Mathf.Abs(rearLeftCollider.rpm - idealRPM);
            float rearRightSlip = Mathf.Abs(rearRightCollider.rpm - idealRPM);
            float avgSlip = (rearLeftSlip + rearRightSlip) / 2f;

            if (avgSlip > 100f && throttleAxis > 0f)
            {
                float reduction = Mathf.Clamp01(avgSlip / 500f) * tractionControlStrength;

                rearLeftCollider.motorTorque *= (1f - reduction);
                rearRightCollider.motorTorque *= (1f - reduction);
                frontLeftCollider.motorTorque *= (1f - reduction);
                frontRightCollider.motorTorque *= (1f - reduction);
            }

            isSkidding = avgSlip > skidSlipThreshold && Mathf.Abs(carSpeed) > 10f;
        }
        else
        {
            isSkidding = false;
        }
    }

    public void StopCar()
    {
        ThrottleOff();
        Brakes();
        carRigidbody.linearVelocity = Vector3.zero;
        carRigidbody.angularVelocity = Vector3.zero;
        this.enabled = false;
    }

    void ThrottleOff()
    {
        frontLeftCollider.motorTorque = 0;
        frontRightCollider.motorTorque = 0;
        rearLeftCollider.motorTorque = 0;
        rearRightCollider.motorTorque = 0;
    }

    void Brakes()
    {
        frontLeftCollider.brakeTorque = brakeForce;
        frontRightCollider.brakeTorque = brakeForce;
        rearLeftCollider.brakeTorque = brakeForce;
        rearRightCollider.brakeTorque = brakeForce;
    }

    public void DecelerateCar()
    {
        // Apply natural deceleration to rigidbody
        carRigidbody.linearVelocity = carRigidbody.linearVelocity * (1f / (1f + (0.025f * decelerationMultiplier)));

        if (carRigidbody.linearVelocity.magnitude < 0.25f)
        {
            carRigidbody.linearVelocity = Vector3.zero;
            CancelInvoke("DecelerateCar");
            deceleratingCar = false;
        }
    }

    // MAIN INPUT METHOD - REFACTORED VERSION
    public void SetInputs(float throttle, float steer, float brake)
    {
        // INVERT THROTTLE: InputManager sends negative for W (forward), positive for S (backward)
        throttle = -throttle;

        steeringAxis = steer;
        throttleAxis = throttle; // Always update throttleAxis to reflect current input

        // Apply steering
        float steeringAngle = steer * maxSteeringAngle;
        if (frontLeftCollider != null)
        {
            frontLeftCollider.steerAngle = Mathf.Lerp(frontLeftCollider.steerAngle, steeringAngle, steeringSpeed);
        }
        else
        {
            Debug.LogError("Front Left WheelCollider is null in PrometeoCarController!");
        }
        if (frontRightCollider != null)
        {
            frontRightCollider.steerAngle = Mathf.Lerp(frontRightCollider.steerAngle, steeringAngle, steeringSpeed);
        }
        else
        {
            Debug.LogError("Front Right WheelCollider is null in PrometeoCarController!");
        }

        float currentMotorTorque = 0f;
        float currentBrakeTorque = 0f;

        // Handle braking when changing direction or explicit brake input
        bool changingDirection = (throttle > 0 && localVelocityZ < -1f) || (throttle < 0 && localVelocityZ > 1f);

        if (changingDirection || brake > 0)
        {
            currentBrakeTorque = brakeForce * (brake > 0 ? brake : 1f);
            currentMotorTorque = 0f; // No motor torque when braking
            isHardBraking = true;
            CancelInvoke("DecelerateCar"); // Stop natural deceleration
            deceleratingCar = false;
        }
        else if (throttle != 0) // Player is actively accelerating or reversing
        {
            isHardBraking = false;
            currentBrakeTorque = 0f; // No brake torque when accelerating

            CancelInvoke("DecelerateCar"); // Stop natural deceleration
            deceleratingCar = false;

            if (throttle > 0) // Forward acceleration (InputManager sends negative for W, so inverted throttle > 0 is reverse in wheel physics)
            {
                if (Mathf.RoundToInt(carSpeed) < maxBackwardSpeed) // Check BACKWARD speed limit
                {
                    currentMotorTorque = throttle * currentAccelerationMultiplier * 100f;
                }
                else
                {
                    currentMotorTorque = 0; // Max backward speed reached
                }
            }
            else if (throttle < 0) // Reverse acceleration (InputManager sends positive for S, so inverted throttle < 0 is forward in wheel physics)
            {
                if (Mathf.Abs(Mathf.RoundToInt(carSpeed)) < currentMaxForwardSpeed) // Check FORWARD speed limit
                {
                    float speedRatio = carSpeed / currentMaxForwardSpeed;
                    float torqueBoost = 1f + (speedRatio * 0.5f);
                    currentMotorTorque = throttle * currentAccelerationMultiplier * 100f * torqueBoost;
                }
                else
                {
                    currentMotorTorque = 0; // Max forward speed reached
                }
            }
        }
        else // No throttle, no active brake, apply natural deceleration
        {
            isHardBraking = false;
            currentMotorTorque = 0f;
            currentBrakeTorque = 0f;

            if (!deceleratingCar && carRigidbody.linearVelocity.magnitude > 0.25f) // Only start if moving and not already decelerating
            {
                InvokeRepeating("DecelerateCar", 0f, 0.1f);
                deceleratingCar = true;
            }
            else if (carRigidbody.linearVelocity.magnitude <= 0.25f && deceleratingCar) // Stop if almost still
            {
                CancelInvoke("DecelerateCar");
                deceleratingCar = false;
                carRigidbody.linearVelocity = Vector3.zero; // Snap to zero to prevent tiny movements
            }
        }

        // Apply calculated torques to all wheels
        if (frontLeftCollider != null) frontLeftCollider.motorTorque = currentMotorTorque;
        if (frontRightCollider != null) frontRightCollider.motorTorque = currentMotorTorque;
        if (rearLeftCollider != null) rearLeftCollider.motorTorque = currentMotorTorque;
        if (rearRightCollider != null) rearRightCollider.motorTorque = currentMotorTorque;

        if (frontLeftCollider != null) frontLeftCollider.brakeTorque = currentBrakeTorque;
        if (frontRightCollider != null) frontRightCollider.brakeTorque = currentBrakeTorque;
        if (rearLeftCollider != null) rearLeftCollider.brakeTorque = currentBrakeTorque;
        if (rearRightCollider != null) rearRightCollider.brakeTorque = currentBrakeTorque;

        BrakingEffectsPS();
    }

    // Power-up methods
    public void SetMaxForwardSpeed(float value) => currentMaxForwardSpeed = value;
    public void SetAccelerationMultiplier(float value) => currentAccelerationMultiplier = value;
    public void SetDownforceCoefficient(float value) => currentDownforceCoefficient = value;
    public void SetAntiRollForceMultiplier(float value) => currentAntiRollForceMultiplier = value;
    public void SetCollisionReduced(bool reduced) => collisionReduced = reduced;

    // Reset all power-up modifications to default values
    public void ResetAllPowerUpModifications()
    {
        currentMaxForwardSpeed = maxForwardSpeed;
        currentAccelerationMultiplier = accelerationMultiplier;
        currentDownforceCoefficient = downforceCoefficient;
        currentAntiRollForceMultiplier = antiRollForceMultiplier;
        collisionReduced = false;
    }

    //EFFECTS
    public void BrakingEffectsPS()
    {
        if (useEffects)
        {
            // Tire smoke during hard braking
            if (isHardBraking && Mathf.Abs(carSpeed) > 30f)
            {
                if (RLWParticleSystem != null) RLWParticleSystem.Play();
                if (RRWParticleSystem != null) RRWParticleSystem.Play();
            }
            else
            {
                if (RLWParticleSystem != null) RLWParticleSystem.Stop();
                if (RRWParticleSystem != null) RRWParticleSystem.Stop();
            }

            // Tire skid marks
            bool shouldSkid = (isHardBraking && Mathf.Abs(carSpeed) > 20f) || (isSkidding && Mathf.Abs(carSpeed) > 10f);
            if (RLWTireSkid != null) RLWTireSkid.emitting = shouldSkid;
            if (RRWTireSkid != null) RRWTireSkid.emitting = shouldSkid;
        }
        else
        {
            if (RLWParticleSystem != null) RLWParticleSystem.Stop();
            if (RRWParticleSystem != null) RRWParticleSystem.Stop();
            if (RLWTireSkid != null) RLWTireSkid.emitting = false;
            if (RRWTireSkid != null) RRWTireSkid.emitting = false;
        }
    }
}
