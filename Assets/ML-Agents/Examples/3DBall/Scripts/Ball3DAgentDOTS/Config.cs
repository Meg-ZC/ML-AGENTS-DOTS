using Unity.Entities;
using UnityEngine;

namespace ML_Agents.Examples._3DBall.Scripts.Ball3DAgentDOTS
{
    public struct ConfigComponent : IComponentData
    {
        public Entity ball;
        public Entity cube;
    }

    public struct InitTag : IComponentData{}
    public class Config : MonoBehaviour
    {
        public GameObject ball;
        public GameObject cube;
        private class ConfigBaker : Baker<Config>
        {
            public override void Bake(Config authoring)
            {
                var com = new ConfigComponent();
                var e = GetEntity(TransformUsageFlags.None);
                com.ball = GetEntity(authoring.ball, TransformUsageFlags.Dynamic);
                com.cube = GetEntity(authoring.cube, TransformUsageFlags.Dynamic);
                AddComponent(e, com);
            }
        }
    }
}

