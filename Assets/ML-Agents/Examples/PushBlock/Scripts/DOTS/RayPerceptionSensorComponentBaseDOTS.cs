using System;
using Unity.MLAgents.Sensors;
using Unity.Physics;
using Unity.Physics.Authoring;
using UnityEngine;
using UnityEngine.Serialization;

namespace ML_Agents.Examples.PushBlock.Scripts.DOTS
{
    public class RayPerceptionSensorComponentBaseDOTS : SensorComponent
    {
        [HideInInspector, SerializeField, FormerlySerializedAs("sensorName")]
        string m_SensorName = "RayPerceptionSensorDOTS";

        public string SensorName
        {
            get { return m_SensorName; }
            set { m_SensorName = value; }
        }

        [SerializeField, FormerlySerializedAs("detectableTags")]
        [Tooltip("List of tags in the scene to compare against.")]
        private CustomPhysicsMaterialTags m_DetectableTags;

        public CustomPhysicsMaterialTags DetectableTags
        {
            get => m_DetectableTags;
            set => m_DetectableTags = value;
        }
        [HideInInspector, SerializeField, FormerlySerializedAs("raysPerDirection")]
        [Range(0, 50)]
        [Tooltip("Number of rays to the left and right of center.")]
        int m_RaysPerDirection = 3;

        public int RaysPerDirection
        {
            get { return m_RaysPerDirection; }
            // Note: can't change at runtime
            set { m_RaysPerDirection = value; }
        }

        [HideInInspector, SerializeField, FormerlySerializedAs("maxRayDegrees")]
        [Range(0, 180)]
        [Tooltip("Cone size for rays. Using 90 degrees will cast rays to the left and right. " +
                 "Greater than 90 degrees will go backwards.")]
        float m_MaxRayDegrees = 70;

        public float MaxRayDegrees
        {
            get => m_MaxRayDegrees;
            set { m_MaxRayDegrees = value; UpdateSensor(); }
        }

        [HideInInspector, SerializeField, FormerlySerializedAs("sphereCastRadius")]
        [Range(0f, 10f)]
        [Tooltip("Radius of sphere to cast. Set to zero for raycasts.")]
        float m_SphereCastRadius = 0.5f;

        public float SphereCastRadius
        {
            get => m_SphereCastRadius;
            set { m_SphereCastRadius = value; UpdateSensor(); }
        }
        [HideInInspector, SerializeField, FormerlySerializedAs("rayLength")]
        [Range(1, 1000)]
        [Tooltip("Length of the rays to cast.")]
        float m_RayLength = 20f;

        public float RayLength
        {
            get => m_RayLength;
            set { m_RayLength = value; UpdateSensor(); }
        }
        const int k_PhysicsDefaultLayers = -5;
        [HideInInspector, SerializeField, FormerlySerializedAs("rayLayerMask")]
        [Tooltip("Controls which layers the rays can hit.")]
        CollisionFilter m_RayLayerMask;

        /// <summary>
        /// Controls which layers the rays can hit.
        /// </summary>
        public CollisionFilter RayLayerMask
        {
            get => m_RayLayerMask;
            set { m_RayLayerMask = value; UpdateSensor(); }
        }

        [HideInInspector, SerializeField, FormerlySerializedAs("observationStacks")]
        [Range(1, 50)]
        [Tooltip("Number of raycast results that will be stacked before being fed to the neural network.")]
        int m_ObservationStacks = 1;

        public int ObservationStacks
        {
            get { return m_ObservationStacks; }
            set { m_ObservationStacks = value; }
        }


        [HideInInspector, SerializeField]
        [Tooltip("Disable to provide the rays in left to right order.  Warning: Alternating order will be deprecated, disable it to ensure compatibility with future versions of ML-Agents.")]
        public bool m_AlternatingRayOrder = true;

        public bool AlternatingRayOrder
        {
            get { return m_AlternatingRayOrder; }
            set { m_AlternatingRayOrder = value; }
        }
        [HideInInspector]
        [SerializeField]
        [Header("Debug Gizmos", order = 999)]
        internal Color rayHitColor = Color.red;

        /// <summary>
        /// Color to code a ray that avoid or misses all other objects.
        /// </summary>
        [HideInInspector]
        [SerializeField]
        internal Color rayMissColor = Color.white;

        [NonSerialized]
        RayPerceptionSensor m_RaySensor;
        public RayPerceptionSensor RaySensor
        {
            get => m_RaySensor;
        }

        public RayPerceptionCastType GetCastType()
        {
            return RayPerceptionCastType.Cast3D;
        }

        /// <summary>
        /// Returns the amount that the ray end is offset up or down by.
        /// </summary>
        /// <returns></returns>
        public virtual float GetEndVerticalOffset()
        {
            return 0f;
        }

        /// <summary>
        /// Returns the amount that the ray start is offset up or down by.
        /// </summary>
        /// <returns></returns>
        public virtual float GetStartVerticalOffset()
        {
            return 0f;
        }

        public override ISensor[] CreateSensors()
        {
            var rayPerceptionInput = GetRayPerceptionInput();

            m_RaySensor = new RayPerceptionSensor(m_SensorName, rayPerceptionInput);

            if (ObservationStacks != 1)
            {
                var stackingSensor = new StackingSensor(m_RaySensor, ObservationStacks);
                return new ISensor[] { stackingSensor };
            }

            return new ISensor[] { m_RaySensor };
        }
        internal static float[] GetRayAnglesAlternating(int raysPerDirection, float maxRayDegrees)
        {
            // Example:
            // { 90, 90 - delta, 90 + delta, 90 - 2*delta, 90 + 2*delta }
            var anglesOut = new float[2 * raysPerDirection + 1];
            var delta = maxRayDegrees / raysPerDirection;
            anglesOut[0] = 90f;
            for (var i = 0; i < raysPerDirection; i++)
            {
                anglesOut[2 * i + 1] = 90 - (i + 1) * delta;
                anglesOut[2 * i + 2] = 90 + (i + 1) * delta;
            }
            return anglesOut;
        }
        internal static float[] GetRayAngles(int raysPerDirection, float maxRayDegrees)
        {
            // Example:
            // { 90 - 3*delta, 90 - 2*delta, ..., 90, 90 + delta, ..., 90 + 3*delta }
            var anglesOut = new float[2 * raysPerDirection + 1];
            var delta = maxRayDegrees / raysPerDirection;

            for (var i = 0; i < 2 * raysPerDirection + 1; i++)
            {
                anglesOut[i] = 90 + (i - raysPerDirection) * delta;
            }

            return anglesOut;
        }
        public RayPerceptionInput GetRayPerceptionInput()
        {
            var rayAngles = m_AlternatingRayOrder ?
                GetRayAnglesAlternating(RaysPerDirection, MaxRayDegrees) :
                GetRayAngles(RaysPerDirection, MaxRayDegrees);

            var rayPerceptionInput = new RayPerceptionInput();
            rayPerceptionInput.RayLength = RayLength;
            // rayPerceptionInput.DetectableTags = DetectableTags.;
            rayPerceptionInput.Angles = rayAngles;
            rayPerceptionInput.StartOffset = GetStartVerticalOffset();
            rayPerceptionInput.EndOffset = GetEndVerticalOffset();
            rayPerceptionInput.CastRadius = SphereCastRadius;
            rayPerceptionInput.Transform = transform;
            rayPerceptionInput.CastType = GetCastType();
            // rayPerceptionInput.LayerMask = (int)RayLayerMask;

            return rayPerceptionInput;
        }

        internal void UpdateSensor()
        {
            if (m_RaySensor != null)
            {
                var rayInput = GetRayPerceptionInput();
                m_RaySensor.SetRayPerceptionInput(rayInput);
            }
        }
            internal int SensorObservationAge()
        {
            if (m_RaySensor != null)
            {
                return Time.frameCount - m_RaySensor.DebugLastFrameCount;
            }

            return 0;
        }
        void OnDrawGizmosSelected()
        {
            if (m_RaySensor?.RayPerceptionOutput?.RayOutputs != null)
            {
                // If we have cached debug info from the sensor, draw that.
                // Draw "old" observations in a lighter color.
                // Since the agent may not step every frame, this helps de-emphasize "stale" hit information.
                var alpha = Mathf.Pow(.5f, SensorObservationAge());

                foreach (var rayInfo in m_RaySensor.RayPerceptionOutput.RayOutputs)
                {
                    DrawRaycastGizmos(rayInfo, alpha);
                }
            }
            else
            {
                var rayInput = GetRayPerceptionInput();
                // We don't actually need the tags here, since they don't affect the display of the rays.
                // Additionally, the user might be in the middle of typing the tag name when this is called,
                // and there's no way to turn off the "Tag ... is not defined" error logs.
                // So just don't use any tags here.
                rayInput.DetectableTags = null;
                for (var rayIndex = 0; rayIndex < rayInput.Angles.Count; rayIndex++)
                {
                    var rayOutput = RayPerceptionSensor.PerceiveSingleRay(rayInput, rayIndex);
                    DrawRaycastGizmos(rayOutput);
                }
            }
        }
        void DrawRaycastGizmos(RayPerceptionOutput.RayOutput rayOutput, float alpha = 1.0f)
        {
            var startPositionWorld = rayOutput.StartPositionWorld;
            var endPositionWorld = rayOutput.EndPositionWorld;
            var rayDirection = endPositionWorld - startPositionWorld;
            rayDirection *= rayOutput.HitFraction;

            // hit fraction ^2 will shift "far" hits closer to the hit color
            var lerpT = rayOutput.HitFraction * rayOutput.HitFraction;
            var color = Color.Lerp(rayHitColor, rayMissColor, lerpT);
            color.a *= alpha;
            Gizmos.color = color;
            Gizmos.DrawRay(startPositionWorld, rayDirection);

            // Draw the hit point as a sphere. If using rays to cast (0 radius), use a small sphere.
            if (rayOutput.HasHit)
            {
                var hitRadius = Mathf.Max(rayOutput.ScaledCastRadius, .05f);
                Gizmos.DrawWireSphere(startPositionWorld + rayDirection, hitRadius);
            }
        }

    }
}
