using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TriggerType { LongCorridorEvent, Puzzle1_ApproachingSkull }

public class TriggerDetector : MonoBehaviour
{
    [SerializeField] private TriggerType triggerType;
    public static event Action<TriggerType, GameObject> OnBackroomTriggerEntered;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            OnBackroomTriggerEntered?.Invoke(triggerType, gameObject);
        }
    }
}