using System;
using UnityEngine;
using UnityEngine.Events;

public class EventTrigger : MonoBehaviour
{
    public UnityEvent Trigger;
    
    public void Triggerfun()
    {
        Trigger?.Invoke();
    }
}
