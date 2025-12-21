using UnityEngine;

[CreateAssetMenu(menuName = "Game/Configs/Experiment Config", fileName = "ExperimentConfig")]
public class ExperimentConfig : ScriptableObject
{
    [Header("Run")]
    [Min(0)] public int requiredSamples = 3;
    public string startSceneName = "StartScreen";
    public string playerTag = "Player";

    [Header("Observation Rule (Mugunghwa)")]
    public bool enableObservationRule = true;
    [Min(0f)] public float stillSpeedThreshold = 0.12f;
    public Vector2 freeMoveDuration = new Vector2(2.5f, 5.5f);
    public Vector2 watchDuration = new Vector2(1.2f, 2.8f);

    [Header("Spawns")]
    public bool spawnSamples = true;
    public bool spawnExit = true;
    [Min(0f)] public float pickupHeight = 0.9f;

    [Header("Audio")]
    public AudioClip samplePickupClip;
    public Vector2 samplePickupVolumeRange = new Vector2(0.7f, 0.9f);
}

