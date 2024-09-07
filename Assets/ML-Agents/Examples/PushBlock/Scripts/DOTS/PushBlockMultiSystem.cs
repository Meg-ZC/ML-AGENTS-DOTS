using System.Data.Common;
using System.Linq;
using Unity.Burst;
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
            var rayLength = raySample[0].GetRayPerceptionInput().RayLength;
            var detectableTags = raySample[0].GetRayPerceptionInput().DetectableTags;

            var ecb = new EntityCommandBuffer(Allocator.Persistent);

            NativeArray<RaycastHit> raycastHits =
                new NativeArray<RaycastHit>(goCount * componentsPerAgent * angle.Length, Allocator.Persistent);
            var offsetArray = new float2x2(new float2(components[0][0].StartVerticalOffset, components[0][0].EndVerticalOffset),
                new float2(components[0][1].StartVerticalOffset, components[0][1].EndVerticalOffset));

            var ltLookup = SystemAPI.GetComponentLookup<LocalTransform>();
            var respawnNativeArray = new NativeArray<bool>(agentsRespawn, Allocator.TempJob);
            var actionNativeArray = new NativeArray<float3>(agentsAction, Allocator.TempJob);
            var rayJob = new RayJob()
            {
                LT = ltLookup,
                PhysicsWorld = SystemAPI.GetSingletonRW<PhysicsWorldSingleton>().ValueRW.PhysicsWorld,
                Angles = angle,
                RayLength = rayLength,
                DetectableTags = detectableTags,
                RayOutputs = raycastHits,
                ComponentPerAgent = componentsPerAgent,
                Offset = offsetArray
            };
            var reSpawnJob = new ReSpawnJob()
            {
                Config = agentConfig,
                ECB = ecb,
                LT = ltLookup,
                Random = m_Random,
                RespawnSignal = respawnNativeArray
            };
            var motionJob = new MotionJob()
            {
                Motion = actionNativeArray,
                RespawnSignal = respawnNativeArray,
                DeltaTime = SystemAPI.Time.fixedDeltaTime
            };
            var collisionEventsJob = new CountNumCollisionEvents()
            {
                Blocks = SystemAPI.GetComponentLookup<PushBlockBlockTagsComponent>(),
                CollisionSignal = respawnNativeArray
            };
            var reSpawnHandle = reSpawnJob.Schedule(Dependency);
            var rayHandle = rayJob.Schedule(Dependency);
            Dependency = collisionEventsJob.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), JobHandle.CombineDependencies(reSpawnHandle, rayHandle));
            Dependency = motionJob.Schedule(Dependency);


            Dependency.Complete();
            for (int i = 0; i < agents.Length; i++)
            {
                agents[i].respawnSignal = respawnNativeArray[i];
                if (respawnNativeArray[i] == true)
                {
                    agents[i].ScoredAGoal();
                }

                for (int j = 0; j < componentsPerAgent; j++)
                {
                    m_Random.NextDouble4();
                    var temArray = raycastHits.GetSubArray(i * componentsPerAgent * angle.Length + j * angle.Length, angle.Length);
                    components[i][j].RaySensor.RayPerceptionOutput.RayOutputs.CopyFrom(temArray);
                }
            }

            ecb.Playback(EntityManager);
            respawnNativeArray.Dispose(Dependency);
            raycastHits.Dispose(Dependency);
            actionNativeArray.Dispose(Dependency);
            ecb.Dispose();

        }

    }

    [BurstCompile]
    public partial struct RayJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> LT;
        [ReadOnly] public PhysicsWorld PhysicsWorld;
        [ReadOnly]public NativeArray<float> Angles;
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
                    RayOutputs[area.ValueRO.Index * Angles.Length * ComponentPerAgent + j * Angles.Length + i] = hit;
                }
            }

        }
    }
    [BurstCompile]
    public partial struct ReSpawnJob : IJobEntity
    {
        [ReadOnly]public ComponentLookup<LocalTransform> LT;
        public EntityCommandBuffer ECB;
        [ReadOnly]public PushBlockConfigComponent Config;
        public Random Random;
        public NativeArray<bool> RespawnSignal;
        private void Execute(RefRO<PushBlockAreaTagsComponent> area,Entity  entity)
        {
            if(RespawnSignal[area.ValueRO.Index] == false)
            {return;}

            RespawnSignal[area.ValueRO.Index] = false;
            NativeList<float3> positions = new NativeList<float3>(2, Allocator.Temp);
            positions.Add(new float3(Random.NextFloat(-11, 11), 0.5f, Random.NextFloat(-7, 11)));
            positions.Add(new float3(Random.NextFloat(-11, 11), 0.5f, Random.NextFloat(-7, 11)));
            while (math.distance(positions[0],positions[1]) < 2)
            {
                positions[1] = new float3(Random.NextFloat(-11, 11), 0.5f, Random.NextFloat(-7, 11));
            }

            positions[0] += LT[entity].Position;
            positions[1] += LT[entity].Position;

            var agentLTPre = LT[area.ValueRO.Agent];
            var blockLTPre = LT[area.ValueRO.Block];

            ECB.DestroyEntity(area.ValueRO.Agent);
            ECB.DestroyEntity(area.ValueRO.Block);

            var a = ECB.Instantiate(Config.Agent);
            var b = ECB.Instantiate(Config.Block);

            ECB.SetComponent(entity,new PushBlockAreaTagsComponent()
            {
                Index = area.ValueRO.Index,
                Agent = a,
                Block = b
            });

            ECB.SetComponent(a,new PushBlockAgentTagsComponent()
            {
                Index = area.ValueRO.Index,
                Block = b
            });

            ECB.SetComponent(b,new PushBlockBlockTagsComponent()
            {
                Index = area.ValueRO.Index
            });

            var agentLT = LocalTransform.Identity;
            agentLT.Position = positions[0];
            if(agentLT.Forward().y > 0.5f)
                agentLT.Rotation = quaternion.identity;
            agentLT.Rotation = agentLTPre.Rotation;
            ECB.SetComponent(a,agentLT);

            var blockLT = LocalTransform.Identity;
            blockLT.Position = positions[1];
            blockLT.Rotation = blockLTPre.Rotation;
            ECB.SetComponent(b,blockLT);

        }
    }
    [BurstCompile]
    public partial struct MotionJob :IJobEntity
    {
        [ReadOnly]public NativeArray<float3> Motion;
        [ReadOnly]public NativeArray<bool> RespawnSignal;
        public float DeltaTime;

        private void Execute(RefRW<LocalTransform> LT, RefRW<PhysicsVelocity> PV, RefRO<PushBlockAgentTagsComponent> agent)
        {
            if(RespawnSignal[agent.ValueRO.Index])
            {
                return;
            }
            var action = Motion[agent.ValueRO.Index];
            var dir = LT.ValueRO.Forward() * action.z * 7f + LT.ValueRO.Right() * action.x * 7f * 0.75f;
            var temPV = PV.ValueRO;
            temPV.Linear *= new float3(0,1,0);
            temPV.Linear += dir;

            PV.ValueRW = temPV;

            LT.ValueRW = LT.ValueRO.RotateY(DeltaTime * Mathf.Deg2Rad * 200f * action.y);
        }
    }
    [BurstCompile]
    public partial struct CountNumCollisionEvents : ICollisionEventsJob
    {
        [ReadOnly]public ComponentLookup<PushBlockBlockTagsComponent> Blocks;
        public NativeArray<bool> CollisionSignal;
        public void Execute(CollisionEvent collisionEvent)
        {
            var a = collisionEvent.EntityA;
            var b = collisionEvent.EntityB;

            if (Blocks.HasComponent(b)
                ||  Blocks.HasComponent(a))
            {
                var block = Blocks.HasComponent(a)?a:b;
                CollisionSignal[Blocks[block].Index] = true;
                Debug.Log($"{Blocks[block].Index}");
            }
        }
    }
}
