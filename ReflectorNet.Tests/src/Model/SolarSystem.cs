using System.Text.Json.Serialization;

namespace com.IvanMurzak.ReflectorNet.Tests.Model
{
    public class SolarSystem
    {
        [System.Serializable]
        public class CelestialBody
        {
            [JsonInclude]
            public GameObjectRef? gameObject;

            [JsonInclude]
            public float orbitRadius = 10f;

            [JsonInclude]
            public float orbitSpeed = 1f;

            [JsonInclude]
            public float rotationSpeed = 1f;

            [JsonInclude]
            public Vector3 orbitTilt = Vector3.zero;
        }

        [JsonInclude]
        public GameObjectRef? sun;

        [JsonInclude]
        public CelestialBody[]? celestialBodies;

        [JsonInclude]
        public float globalOrbitSpeedMultiplier = 1f;

        [JsonInclude]
        public float globalSizeMultiplier = 1f;
    }
}