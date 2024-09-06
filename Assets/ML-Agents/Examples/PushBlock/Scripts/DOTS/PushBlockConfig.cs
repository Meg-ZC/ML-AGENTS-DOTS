using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace ML_Agents.Examples.PushBlock.Scripts.DOTS
{
    public class PushBlockConfig : MonoBehaviour
    {
        public GameObject Agent;
        public GameObject Block;
        public GameObject Area;

        private class PushBlockConfigBaker : Baker<PushBlockConfig>
        {
            public override void Bake(PushBlockConfig authoring)
            {
                var e = GetEntity(TransformUsageFlags.None);
                AddComponent(e,new PushBlockConfigComponent()
                {
                    Agent = GetEntity(authoring.Agent,TransformUsageFlags.Dynamic),
                    Block = GetEntity(authoring.Block,TransformUsageFlags.Dynamic),
                    Area = GetEntity(authoring.Area,TransformUsageFlags.Dynamic)
                });
            }
        }
    }

    public struct PushBlockConfigComponent : IComponentData
    {
        public Entity Agent;
        public Entity Block;
        public Entity Area;
    }
}

