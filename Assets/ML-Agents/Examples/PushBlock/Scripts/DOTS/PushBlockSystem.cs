using System.Linq;
using ML_Agents.Examples.PushBlock.Scripts.DOTS;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.MLAgents;
using Unity.Physics;
using Unity.Physics.Aspects;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace ML_Agents.Examples.PushBlock.Scripts.DOTS
{

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSimulationGroup))]
    public partial class PushBlockSystem : SystemBase
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

            var agent = GameObject.FindWithTag("agent");
            var rayComponents = agent.GetComponents<RayPerceptionSensorComponentDOTS>();
            var agentComponent = agent.GetComponent<FakePushBlockAgent>();
            var config = SystemAPI.GetSingleton<PushBlockConfigComponent>();

            EntityQuery query = SystemAPI.QueryBuilder().WithAll<BlockGoalCollisionSignal>().Build();
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            if (!query.IsEmpty)
            {
                EntityManager.DestroyEntity(query);
                agentComponent.ScoredAGoal();
            }
            else
            {
                Dependency = new CountNumCollisionEvents
                {
                    ECB = ecb,
                    Blocks = GetComponentLookup<PushBlockBlockTagsComponent>(),
                    Goals = GetComponentLookup<PushBlockGoalTagsComponent>()
                }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(),Dependency);
            }

            if(rayComponents == null) return;
            foreach (var c in rayComponents)
            {
                if(c.IsInit == false) return;
            }

            PhysicsWorld world = SystemAPI.GetSingletonRW<PhysicsWorldSingleton>().ValueRW.PhysicsWorld;

            foreach (var (LT,PBA,PV,PM,e) in SystemAPI.Query<RefRW<LocalTransform>,RefRO<PushBlockAgentTagsComponent>,RefRW<PhysicsVelocity>,RefRO<PhysicsMass>>().WithEntityAccess())
            {
                if (agentComponent.respawnSignal)
                {
                    Dependency.Complete();
                    agentComponent.respawnSignal = false;
                    NativeList<float3> positions = new NativeList<float3>(3, Allocator.Temp);
                    //z -8 12
                    //x -12 12
                    var randomPosZ = m_Random.NextFloat(-7, 11);
                    var randomPosX = m_Random.NextFloat(-11, 11);
                    positions.Add(new float3(randomPosX, 1, randomPosZ));
                    positions.Add(new float3(m_Random.NextFloat(-7, 11), 1, m_Random.NextFloat(-11, 11)));
                    while (math.distance(positions[0],positions[1]) < 2)
                    {
                        positions[1] = new float3(m_Random.NextFloat(-7, 11), 1, m_Random.NextFloat(-11, 11));
                    }
                    ecb.DestroyEntity(PBA.ValueRO.Block);
                    ecb.DestroyEntity(e);

                    var a = ecb.Instantiate(config.Agent);
                    var b = ecb.Instantiate(config.Block);

                    ecb.SetComponent(a,new PushBlockAgentTagsComponent(){Block = b});

                    var agentLT = LocalTransform.Identity;
                    agentLT.Position = positions[0];
                    agentLT.Rotation = LT.ValueRO.Rotation;

                    var blockLT = LocalTransform.Identity;
                    blockLT.Position = positions[1];

                    ecb.SetComponent(b,blockLT);
                    ecb.SetComponent(a,agentLT);
                }
                else
                {
                    foreach (var c in rayComponents)
                    {
                        var rays = c.RaySensor;
                        var rayOutputs = rays.RayPerceptionOutput;
                        var rayInput = rays.RayPerceptionInput;

                        NativeArray<RaycastInput> inputs = new NativeArray<RaycastInput>(rayInput.Angles.Count, Allocator.Temp);
                        for (int i = 0; i < rayInput.Angles.Count; i++)
                        {
                            var input = inputs[i];
                            var temLT = LT.ValueRO;
                            input.Start = temLT.Position + new float3(0,rayInput.StartOffset,0);
                            temLT = temLT.RotateY(Mathf.Deg2Rad * (rayInput.Angles[i] - 90));
                            input.End = temLT.Position + new float3(0,rayInput.EndOffset,0) + temLT.Forward() * rayInput.RayLength;
                            var filter = CollisionFilter.Default;
                            filter.CollidesWith = ~1u;
                            input.Filter = filter;
                            inputs[i] = input;

                            var isHit = world.CastRay(input,out var raycastHit);
                            raycastHit.Fraction = isHit ? raycastHit.Fraction : 1f;
                            rayOutputs.RayOutputs[i] = raycastHit;
                            var tag = raycastHit.Material.CustomTags;

                            Color color = Color.black;
                            if(tag == 4) color = Color.green;
                            if(tag == 8) color = Color.red;

                            Debug.DrawRay(input.Start, isHit?(raycastHit.Position - input.Start):(input.End - input.Start),color);
                        }

                    }

                    var temPV = PV.ValueRO;
                    var forwardDir = LT.ValueRO.Forward();
                    forwardDir *= agentComponent.action2DOTS.z * 5f;
                    var rightDir = LT.ValueRO.Right();
                    rightDir *= agentComponent.action2DOTS.x * 5f * 0.75f;
                    temPV.Linear *= new float3(0, 1, 0);
                    temPV.Linear += forwardDir;
                    temPV.Linear += rightDir;
                    PV.ValueRW = temPV;

                    LT.ValueRW = LT.ValueRO.RotateY(SystemAPI.Time.fixedDeltaTime * Mathf.Deg2Rad * 200f * agentComponent.action2DOTS.y);

                }
            }

            Dependency.Complete();
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }

    [BurstCompile]
    public partial struct CountNumCollisionEvents : ICollisionEventsJob
    {
        public EntityCommandBuffer ECB;
        [ReadOnly]public ComponentLookup<PushBlockBlockTagsComponent> Blocks;
        [ReadOnly]public ComponentLookup<PushBlockGoalTagsComponent> Goals;
        public void Execute(CollisionEvent collisionEvent)
        {
            var a = collisionEvent.EntityA;
            var b = collisionEvent.EntityB;

            if ((Goals.HasComponent(a) && Blocks.HasComponent(b))
                || (Goals.HasComponent(b) && Blocks.HasComponent(a)))
            {
                var e = ECB.CreateEntity();
                ECB.AddComponent<BlockGoalCollisionSignal>(e);
            }
        }
    }
}
