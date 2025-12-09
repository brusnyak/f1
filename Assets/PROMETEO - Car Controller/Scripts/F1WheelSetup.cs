using UnityEngine;

[RequireComponent(typeof(PrometeoCarController))]
public class F1WheelSetup : MonoBehaviour
{
    [Header("Click to apply F1 grip settings to all wheels")]
    [Space(10)]
    public bool applyF1Settings = false;

    [Header("F1 Friction Settings")]
    [Space(5)]
    [Range(0.1f, 1.0f)]
    public float extremumSlip = 0.3f; // How much slip before max friction (lower = grippier)
    
    [Range(1.0f, 4.0f)]
    public float extremumValue = 2.5f; // Maximum friction force (higher = more grip)
    
    [Range(0.5f, 2.0f)]
    public float asymptoteSlip = 0.8f; // Slip at which friction falls off
    
    [Range(0.75f, 3.0f)]
    public float asymptoteValue = 2.0f; // Friction at high slip
    
    [Range(1.0f, 5.0f)]
    public float stiffness = 3.0f; // Overall grip multiplier (higher = much grippier!)

    [Header("Wheel Collider Mass")]
    [Range(10f, 50f)]
    public float wheelMass = 20f; // F1 wheels are lightweight

    private PrometeoCarController carController;

    void Start()
    {
        carController = GetComponent<PrometeoCarController>();
        
        // Auto-apply on start if enabled
        if(applyF1Settings)
        {
            ApplyF1GripSettings();
        }
    }

    void Update()
    {
        // Allow runtime adjustment
        if(applyF1Settings)
        {
            ApplyF1GripSettings();
            applyF1Settings = false; // Reset checkbox
        }
    }

    public void ApplyF1GripSettings()
    {
        if(carController == null)
        {
            carController = GetComponent<PrometeoCarController>();
        }

        Debug.Log("Applying F1 grip settings to all wheels...");

        // Apply to all 4 wheels
        ApplyToWheel(carController.frontLeftCollider, "Front Left");
        ApplyToWheel(carController.frontRightCollider, "Front Right");
        ApplyToWheel(carController.rearLeftCollider, "Rear Left");
        ApplyToWheel(carController.rearRightCollider, "Rear Right");

        Debug.Log("F1 grip settings applied successfully!");
    }

    void ApplyToWheel(WheelCollider wheel, string wheelName)
    {
        if(wheel == null)
        {
            Debug.LogWarning($"{wheelName} wheel collider not assigned!");
            return;
        }

        // Sideways friction (for cornering grip)
        WheelFrictionCurve sidewaysFriction = wheel.sidewaysFriction;
        sidewaysFriction.extremumSlip = extremumSlip;
        sidewaysFriction.extremumValue = extremumValue;
        sidewaysFriction.asymptoteSlip = asymptoteSlip;
        sidewaysFriction.asymptoteValue = asymptoteValue;
        sidewaysFriction.stiffness = stiffness;
        wheel.sidewaysFriction = sidewaysFriction;

        // Forward friction (for acceleration/braking grip)
        WheelFrictionCurve forwardFriction = wheel.forwardFriction;
        forwardFriction.extremumSlip = extremumSlip;
        forwardFriction.extremumValue = extremumValue;
        forwardFriction.asymptoteSlip = asymptoteSlip;
        forwardFriction.asymptoteValue = asymptoteValue;
        forwardFriction.stiffness = stiffness;
        wheel.forwardFriction = forwardFriction;

        // Set wheel mass
        wheel.mass = wheelMass;

        Debug.Log($"{wheelName}: Stiffness={stiffness}, ExtremumSlip={extremumSlip}, Mass={wheelMass}");
    }

    // Helper method to show current settings
    [ContextMenu("Show Current Wheel Settings")]
    public void ShowCurrentSettings()
    {
        if(carController == null)
        {
            carController = GetComponent<PrometeoCarController>();
        }

        Debug.Log("=== CURRENT WHEEL SETTINGS ===");
        LogWheelSettings(carController.frontLeftCollider, "Front Left");
        LogWheelSettings(carController.frontRightCollider, "Front Right");
        LogWheelSettings(carController.rearLeftCollider, "Rear Left");
        LogWheelSettings(carController.rearRightCollider, "Rear Right");
    }

    void LogWheelSettings(WheelCollider wheel, string wheelName)
    {
        if(wheel == null) return;

        Debug.Log($"{wheelName} Wheel:");
        Debug.Log($"  Sideways - Stiffness: {wheel.sidewaysFriction.stiffness}, ExtremumSlip: {wheel.sidewaysFriction.extremumSlip}, ExtremumValue: {wheel.sidewaysFriction.extremumValue}");
        Debug.Log($"  Forward - Stiffness: {wheel.forwardFriction.stiffness}, ExtremumSlip: {wheel.forwardFriction.extremumSlip}");
        Debug.Log($"  Mass: {wheel.mass}");
    }
}
