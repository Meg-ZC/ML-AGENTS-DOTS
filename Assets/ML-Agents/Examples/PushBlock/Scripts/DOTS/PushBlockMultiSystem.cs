using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;
using RaycastHit = Unity.Physics.RaycastHit;

namespace ML_Agents.Examples.PushBlock.Scripts.DOTS
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSimulationGroup))]
    public partial class PushBlockMultiSystem : SystemBase
    {
        private Random m_Random;
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Random = new Random(1);
            RequireForUpdate<PushBlockAgentTagsComponent>();
        }

        protected override void OnUpdate()
        {
            var agentConfig = SystemAPI.GetSingleton<PushBlockConfigComponent>();
            var go = GameObject.FindGameObjectsWithTag("agent");
            var goCount = go.Length;
            var raySample = go[0].GetComponents<RayPerceptionSensorComponentDOTS>();
            var rayPerGo = raySample.Length;
            var components = go.Select(g => g.GetComponents<RayPerceptionSensorComponentDOTS>()).ToArray();
            var componentsPerAgent = components[0].Length;
            var agents = go.Select(g => g.GetComponent<FakePushBlockAgent>()).ToArray();
            var agentsAction = agents.Select(g => g.action2DOTS).ToArray();
            var agentsRespawn = agents.Select(g => g.respawnSignal).ToArray();

            var angle = raySample[0].GetRayPerceptionInput().Angles;
            var startOffset = raySample[0].GetRayPerceptionInput().StartOffset;
            var endOffset = raySample[0].GetRayPerceptionInput().EndOffset;
            var rayLength = raySample[0].GetRayPerceptionInput().RayLength;
            var detectableTags = raySample[0].GetRayPerceptionInput().DetectableTags;

            var ecb = new EntityCommandBuffer(Allocator.Persistent);

            NativeArray<RaycastHit> raycastHits =
                new NativeArray<RaycastHit>(goCount * componentsPerAgent * angle.Length, Allocator.Persistent);
            var offsetArray = new float2x2(new float2(components[0][0].StartVerticalOffset, components[0][0].EndVerticalOffset),
                new float2(components[0][1].StartVerticalOffset, components[0][1].EndVerticalOffset));

            var LTLookup = SystemAPI.GetComponentLookup<LocalTransform>();
            var rayJob = new RayJob()
            {
                LT = LTLookup,
                PhysicsWorld = SystemAPI.GetSingletonRW<PhysicsWorldSingleton>().ValueRW.PhysicsWorld,
                Angles = angle,
                RayLength = rayLength,
                DetectableTags = detectableTags,
                RayOutputs = raycastHits,
                ComponentPerAgent = componentsPerAgent,
                Offset = offsetArray
            };
            var respawnNativeArray = new NativeArray<bool>(agentsRespawn, Allocator.TempJob);
            var ReSpawnJob = new ReSpawnJob()
            {
                Config = agentConfig,
                ECB = ecb,
                LT = LTLookup,
                Random = m_Random,
                RespawnSignal = respawnNativeArray
            };
            Dependency = ReSpawnJob.Schedule(Dependency);
            Dependency = rayJob.Schedule(Dependency);


            Dependency.Complete();
            for (int i = 0; i < agents.Length; i++)
            {
                agents[i].respawnSignal = respawnNativeArray[i];
            }

            ecb.Playback(EntityManager);
            respawnNativeArray.Dispose(Dependency);
            raycastHits.Dispose(Dependency);
            ecb.Dispose();

        }

    }

    public partial struct RayJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> LT;
        [ReadOnly] public PhysicsWorld PhysicsWorld;
        public NativeArray<float> Angles;
        public float RayLength;
        public CustomPhysicsMaterialTags DetectableTags;

        public NativeArray<RaycastHit> RayOutputs;
        public int ComponentPerAgent;
        public float2x2 Offset;
        private void Execute(RefRO<PushBlockAreaTagsComponent> area)
        {
            var agentLT = LT[area.ValueRO.Agent];
            for (int j = 0; j < ComponentPerAgent; j++)
            {
                for (int i = 0; i < Angles.Length; i++)
                {
                    var temLT = agentLT;
                    temLT = temLT.RotateY(Mathf.Deg2Rad * (Angles[i] - 90));

                    RaycastInput input = new RaycastInput()
                    {
                        Start = temLT.Position + new float3(0,Offset[j][0],0),
                        End = temLT.Position + new float3(0,Offset[j][1],0) + temLT.Forward() * RayLength,
                        Filter = new CollisionFilter()
                        {
                            BelongsTo = (uint)~DetectableTags.Value,
                            CollidesWith = DetectableTags.Value,
                            GroupIndex = 0
                        }
                    };
                    var isHit = PhysicsWorld.CastRay(input, out RaycastHit hit);
                    hit.Fraction = isHit ? hit.Fraction : 1f;
                    RayOutputs[area.ValueRO.Index * Angles.Length * 2 + j * Angles.Length + i] = hit;

                    var tag = hit.Material.CustomTags;

                    Color color = Color.black;
                    if(tag == 4) color = Color.green;
                    if(tag == 8) color = Color.red;

                    Debug.DrawRay(input.Start, isHit?(hit.Position - input.Start):(input.End - input.Start),color);

                }
            }

        }
    }

    public partial struct ReSpawnJob : IJobEntity
    {
        [ReadOnly]public ComponentLookup<LocalTransform> LT;
        public EntityCommandBuffer ECB;
        public PushBlockConfigComponent Config;
        public Random Random;
        public NativeArray<bool> RespawnSignal;
        private void Execute(RefRO<PushBlockAreaTagsComponent> area,in Entity entity)
        {
            if(RespawnSignal[area.ValueRO.Index] == false)
            {return;}

            RespawnSignal[area.ValueRO.Index] = false;
            NativeList<float3> positions = new NativeList<float3>(2, Allocator.Temp);
            positions.Add(new float3(Random.NextFloat(-7, 11), 1, Random.NextFloat(-11, 11)));
            positions.Add(new float3(Random.NextFloat(-7, 11), 1, Random.NextFloat(-11, 11)));
            while (math.distance(positions[0],positions[1]) < 2)
            {
                positions[1] = new float3(Random.NextFloat(-7, 11), 1, Random.NextFloat(-11, 11));
            }

            positions[0] += LT[entity].Position;
            positions[1] += LT[entity].Position;

            var AgentLTPre = LT[area.ValueRO.Agent];
            var BlockLTPre = LT[area.ValueRO.Block];

            ECB.DestroyEntity(area.ValueRO.Agent);
            ECB.DestroyEntity(area.ValueRO.Block);

            var a = ECB.Instantiate(Config.Agent);
            var b = ECB.Instantiate(Config.Block);

            ECB.SetComponent(entity,new PushBlockAreaTagsComponent()
            {
                Agent = a,
                Block = b
            });

            var agentLT = LocalTransform.Identity;
            agentLT.Position = positions[0];
            agentLT.Rotation = AgentLTPre.Rotation;
            ECB.SetComponent(a,agentLT);

            var blockLT = LocalTransform.Identity;
            blockLT.Position = positions[1];
            blockLT.Rotation = BlockLTPre.Rotation;
            ECB.SetComponent(b,blockLT);

        }
    }


}
