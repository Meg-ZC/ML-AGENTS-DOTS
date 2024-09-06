using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.PlayerLoop;

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

            var g = GameObject.FindGameObjectsWithTag("agent").Length;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var config = SystemAPI.GetSingleton<PushBlockConfigComponent>();

            var baseTransformOfAgent = SystemAPI.GetComponent<LocalTransform>(config.Agent);
            var baseTransformOfBlock = SystemAPI.GetComponent<LocalTransform>(config.Block);
            var baseTransformOfArea = SystemAPI.GetComponent<LocalTransform>(config.Area);

            for (int i = 0; i < g; i++)
            {
                float3 offset = new float3(24 * i, 0, 0);
                var agent = ecb.Instantiate(config.Agent);
                var block = ecb.Instantiate(config.Block);
                var area = ecb.Instantiate(config.Area);

                var agentTransform = baseTransformOfAgent;
                agentTransform.Position += offset;
                ecb.SetComponent(agent, agentTransform);
                ecb.SetComponent(agent, new PushBlockAgentTagsComponent { Block = block });

                var blockTransform = baseTransformOfBlock;
                blockTransform.Position += offset;
                ecb.SetComponent(block, blockTransform);

                var areaTransform = baseTransformOfArea;
                areaTransform.Position += offset;
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
