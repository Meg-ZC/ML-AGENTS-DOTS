using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.PlayerLoop;
using RaycastHit = Unity.Physics.RaycastHit;

namespace ML_Agents.Examples.PushBlock.Scripts.DOTS
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct SpawnInitSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PushBlockConfigComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            var agents = GameObject.FindGameObjectsWithTag("agent");
            var g = agents.Length;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var config = SystemAPI.GetSingleton<PushBlockConfigComponent>();

            var baseTransformOfAgent = SystemAPI.GetComponent<LocalTransform>(config.Agent);
            var baseTransformOfBlock = SystemAPI.GetComponent<LocalTransform>(config.Block);
            var baseTransformOfArea = SystemAPI.GetComponent<LocalTransform>(config.Area);

            for (int i = 0; i < g; i++)
            {
                float3 offset = new float3(25 * i, 0, 0);
                var agent = ecb.Instantiate(config.Agent);
                var block = ecb.Instantiate(config.Block);
                var area = ecb.Instantiate(config.Area);

                var agentTransform = baseTransformOfAgent;
                agentTransform.Position += offset;
                ecb.SetComponent(agent, agentTransform);
                ecb.SetComponent(agent, new PushBlockAgentTagsComponent { Block = block ,Index = i});

                var blockTransform = baseTransformOfBlock;
                blockTransform.Position += offset;
                ecb.SetComponent(block, blockTransform);

                var areaTransform = baseTransformOfArea;
                areaTransform.Position += offset;

                var components = agents[i].GetComponents<RayPerceptionSensorComponentDOTS>();

                var inputs = new PushBlockAreaTagsComponent()
                {
                    Index = i,
                    Agent = agent,
                    Block = block
                };
                ecb.SetComponent(area,inputs);
                ecb.SetComponent(area, areaTransform);
            }
            ecb.Playback(state.EntityManager);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
