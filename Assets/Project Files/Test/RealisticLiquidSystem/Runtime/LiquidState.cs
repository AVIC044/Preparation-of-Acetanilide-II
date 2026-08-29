using UnityEngine;

namespace RealisticLiquidSystem
{
    public enum LiquidState { Idle, Filling, Sloshing, Pouring, Splashing, Draining, Reset }

    [System.Serializable]
    public struct FluidParticle
    {
        public Vector3 position;
        public Vector3 velocity;
        public float density;
        public float pressure;
        public bool active;
    }
}
