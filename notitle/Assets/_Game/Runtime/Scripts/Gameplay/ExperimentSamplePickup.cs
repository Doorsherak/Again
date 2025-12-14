using UnityEngine;

[DisallowMultipleComponent]
public class ExperimentSamplePickup : MonoBehaviour
{
    [HideInInspector] public ExperimentBootstrap bootstrap;

    void OnTriggerEnter(Collider other)
    {
        if (!bootstrap) return;
        if (!other.CompareTag("Player")) return;
        bootstrap.OnCollectedSample();
        Destroy(gameObject);
    }
}

