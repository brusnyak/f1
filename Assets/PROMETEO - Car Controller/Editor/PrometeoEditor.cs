using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;

[CustomEditor(typeof(PrometeoCarController))]
[System.Serializable]
public class PrometeoEditor : Editor{

  enum displayFieldType {DisplayAsAutomaticFields, DisplayAsCustomizableGUIFields}
  displayFieldType DisplayFieldType;

  private PrometeoCarController prometeo;
  private SerializedObject SO;
  //
  //
  //CAR SETUP
  //
  //
  private SerializedProperty maxBackwardSpeed;
  private SerializedProperty maxForwardSpeed;
  private SerializedProperty accelerationMultiplier;
  private SerializedProperty maxSteeringAngle;
  private SerializedProperty steeringSpeed;
  private SerializedProperty brakeForce;
  private SerializedProperty decelerationMultiplier;
  private SerializedProperty bodyMassCenter;
  //F1 PHYSICS
  private SerializedProperty useDownforce;
  private SerializedProperty downforceCoefficient;
  private SerializedProperty useTractionControl;
  private SerializedProperty tractionControlStrength;
  private SerializedProperty brakingEffectThreshold;
  //
  //
  //WHEELS VARIABLES
  //
  //
  private SerializedProperty frontLeftMesh;
  private SerializedProperty frontLeftCollider;
  private SerializedProperty frontRightMesh;
  private SerializedProperty frontRightCollider;
  private SerializedProperty rearLeftMesh;
  private SerializedProperty rearLeftCollider;
  private SerializedProperty rearRightMesh;
  private SerializedProperty rearRightCollider;
  //
  //
  //PARTICLE SYSTEMS' VARIABLES
  //
  //
  private SerializedProperty useEffects;
  private SerializedProperty RLWParticleSystem;
  private SerializedProperty RRWParticleSystem;
  private SerializedProperty RLWTireSkid;
  private SerializedProperty RRWTireSkid;
  //
  //
  //SPEED TEXT (UI) VARIABLES
  //
  //
  private SerializedProperty useUI;
  private SerializedProperty carSpeedText;
  //
  //
  //SPEED TEXT (UI) VARIABLES
  //
  //
  private SerializedProperty useSounds;
  private SerializedProperty carEngineSound;
  private SerializedProperty tireScreechSound;
  //
  //
  //TOUCH CONTROLS VARIABLES
  //
  //
  // Removed touch control properties as they are no longer in PrometeoCarController.cs

  private void OnEnable(){
    prometeo = (PrometeoCarController)target;
    SO = new SerializedObject(target);

    maxBackwardSpeed = SO.FindProperty("maxBackwardSpeed");
    maxForwardSpeed = SO.FindProperty("maxForwardSpeed");
    accelerationMultiplier = SO.FindProperty("accelerationMultiplier");
    maxSteeringAngle = SO.FindProperty("maxSteeringAngle");
    steeringSpeed = SO.FindProperty("steeringSpeed");
    brakeForce = SO.FindProperty("brakeForce");
    decelerationMultiplier = SO.FindProperty("decelerationMultiplier");
    bodyMassCenter = SO.FindProperty("bodyMassCenter");
    useDownforce = SO.FindProperty("useDownforce");
    downforceCoefficient = SO.FindProperty("downforceCoefficient");
    useTractionControl = SO.FindProperty("useTractionControl");
    tractionControlStrength = SO.FindProperty("tractionControlStrength");
    brakingEffectThreshold = SO.FindProperty("brakingEffectThreshold");

    frontLeftMesh = SO.FindProperty("frontLeftMesh");
    frontLeftCollider = SO.FindProperty("frontLeftCollider");
    frontRightMesh = SO.FindProperty("frontRightMesh");
    frontRightCollider = SO.FindProperty("frontRightCollider");
    rearLeftMesh = SO.FindProperty("rearLeftMesh");
    rearLeftCollider = SO.FindProperty("rearLeftCollider");
    rearRightMesh = SO.FindProperty("rearRightMesh");
    rearRightCollider = SO.FindProperty("rearRightCollider");

    useEffects = SO.FindProperty("useEffects");
    RLWParticleSystem = SO.FindProperty("RLWParticleSystem");
    RRWParticleSystem = SO.FindProperty("RRWParticleSystem");
    RLWTireSkid = SO.FindProperty("RLWTireSkid");
    RRWTireSkid = SO.FindProperty("RRWTireSkid");

    useUI = SO.FindProperty("useUI");
    carSpeedText = SO.FindProperty("carSpeedText");

    useSounds = SO.FindProperty("useSounds");
    carEngineSound = SO.FindProperty("carEngineSound");
    tireScreechSound = SO.FindProperty("tireScreechSound");

    // Removed touch control properties as they are no longer in PrometeoCarController.cs
    // useTouchControls = SO.FindProperty("useTouchControls");
    // throttleButton = SO.FindProperty("throttleButton");
    // reverseButton = SO.FindProperty("reverseButton");
    // turnRightButton = SO.FindProperty("turnRightButton");
    // turnLeftButton = SO.FindProperty("turnLeftButton");

  }

  public override void OnInspectorGUI(){

    SO.Update();

    GUILayout.Space(25);
    GUILayout.Label("F1 PERFORMANCE", EditorStyles.boldLabel);
    GUILayout.Space(10);
    //
    //
    //F1 PERFORMANCE SETUP
    //
    //
    //
    maxBackwardSpeed.intValue = EditorGUILayout.IntSlider("Max Backward Speed (km/h):", maxBackwardSpeed.intValue, 10, 120);
    maxForwardSpeed.intValue = EditorGUILayout.IntSlider("Max Forward Speed:", maxForwardSpeed.intValue, 20, 400);
    accelerationMultiplier.intValue = EditorGUILayout.IntSlider("Acceleration Multiplier:", accelerationMultiplier.intValue, 1, 20);
    maxSteeringAngle.intValue = EditorGUILayout.IntSlider("Max Steering Angle:", maxSteeringAngle.intValue, 10, 45);
    steeringSpeed.floatValue = EditorGUILayout.Slider("Steering Speed:", steeringSpeed.floatValue, 0.1f, 1f);
    brakeForce.intValue = EditorGUILayout.IntSlider("Brake Force:", brakeForce.intValue, 100, 3000);
    decelerationMultiplier.intValue = EditorGUILayout.IntSlider("Deceleration Multiplier:", decelerationMultiplier.intValue, 1, 10);
    EditorGUILayout.PropertyField(bodyMassCenter, new GUIContent("Mass Center of Car: "));

    GUILayout.Space(25);
    GUILayout.Label("F1 PHYSICS SYSTEMS", EditorStyles.boldLabel);
    GUILayout.Space(10);
    
    useDownforce.boolValue = EditorGUILayout.Toggle("Use Downforce:", useDownforce.boolValue);
    downforceCoefficient.floatValue = EditorGUILayout.Slider("Downforce Coefficient:", downforceCoefficient.floatValue, 0.5f, 5f);
    GUILayout.Space(5);
    useTractionControl.boolValue = EditorGUILayout.Toggle("Use Traction Control:", useTractionControl.boolValue);
    tractionControlStrength.floatValue = EditorGUILayout.Slider("Traction Control Strength:", tractionControlStrength.floatValue, 0f, 1f);

    //
    //
    //WHEELS
    //
    //

    GUILayout.Space(25);
    GUILayout.Label("WHEELS", EditorStyles.boldLabel);
    GUILayout.Space(10);

    EditorGUILayout.PropertyField(frontLeftMesh, new GUIContent("Front Left Mesh: "));
    EditorGUILayout.PropertyField(frontLeftCollider, new GUIContent("Front Left Collider: "));

    EditorGUILayout.PropertyField(frontRightMesh, new GUIContent("Front Right Mesh: "));
    EditorGUILayout.PropertyField(frontRightCollider, new GUIContent("Front Right Collider: "));

    EditorGUILayout.PropertyField(rearLeftMesh, new GUIContent("Rear Left Mesh: "));
    EditorGUILayout.PropertyField(rearLeftCollider, new GUIContent("Rear Left Collider: "));

    EditorGUILayout.PropertyField(rearRightMesh, new GUIContent("Rear Right Mesh: "));
    EditorGUILayout.PropertyField(rearRightCollider, new GUIContent("Rear Right Collider: "));

    //
    //
    //EFFECTS
    //
    //

    GUILayout.Space(25);
    GUILayout.Label("EFFECTS", EditorStyles.boldLabel);
    GUILayout.Space(10);

    useEffects.boolValue = EditorGUILayout.BeginToggleGroup("Use effects (braking effects)?", useEffects.boolValue);
    GUILayout.Space(10);

        EditorGUILayout.PropertyField(RLWParticleSystem, new GUIContent("Rear Left Particle System: "));
        EditorGUILayout.PropertyField(RRWParticleSystem, new GUIContent("Rear Right Particle System: "));

        EditorGUILayout.PropertyField(RLWTireSkid, new GUIContent("Rear Left Trail Renderer: "));
        EditorGUILayout.PropertyField(RRWTireSkid, new GUIContent("Rear Right Trail Renderer: "));
        
        brakingEffectThreshold.floatValue = EditorGUILayout.Slider("Braking Effect Threshold:", brakingEffectThreshold.floatValue, 0.3f, 1f);

    EditorGUILayout.EndToggleGroup();

    //
    //
    //UI
    //
    //

    GUILayout.Space(25);
    GUILayout.Label("UI", EditorStyles.boldLabel);
    GUILayout.Space(10);

    useUI.boolValue = EditorGUILayout.BeginToggleGroup("Use UI (Speed text)?", useUI.boolValue);
    GUILayout.Space(10);

        EditorGUILayout.PropertyField(carSpeedText, new GUIContent("Speed Text (UI): "));

    EditorGUILayout.EndToggleGroup();

    //
    //
    //SOUNDS
    //
    //

    GUILayout.Space(25);
    GUILayout.Label("SOUNDS", EditorStyles.boldLabel);
    GUILayout.Space(10);

    useSounds.boolValue = EditorGUILayout.BeginToggleGroup("Use sounds (car sounds)?", useSounds.boolValue);
    GUILayout.Space(10);

        EditorGUILayout.PropertyField(carEngineSound, new GUIContent("Car Engine Sound: "));
        EditorGUILayout.PropertyField(tireScreechSound, new GUIContent("Tire Screech Sound: "));

    EditorGUILayout.EndToggleGroup();

    //
    //
    //TOUCH CONTROLS
    //
    //
    // Removed touch control section as it is no longer in PrometeoCarController.cs
    // GUILayout.Space(25);
    // GUILayout.Label("TOUCH CONTROLS", EditorStyles.boldLabel);
    // GUILayout.Space(10);

    // useTouchControls.boolValue = EditorGUILayout.BeginToggleGroup("Use touch controls (mobile devices)?", useTouchControls.boolValue);
    // GUILayout.Space(10);

    //     EditorGUILayout.PropertyField(throttleButton, new GUIContent("Throttle Button: "));
    //     EditorGUILayout.PropertyField(reverseButton, new GUIContent("Brakes/Reverse Button: "));
    //     EditorGUILayout.PropertyField(turnLeftButton, new GUIContent("Turn Left Button: "));
    //     EditorGUILayout.PropertyField(turnRightButton, new GUIContent("Turn Right Button: "));

    // EditorGUILayout.EndToggleGroup();

    //END

    GUILayout.Space(10);
    SO.ApplyModifiedProperties();

  }

}
