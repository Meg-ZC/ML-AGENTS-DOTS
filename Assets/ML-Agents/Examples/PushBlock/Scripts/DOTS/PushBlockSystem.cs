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

namespace ML_Agents.Examples.PushBlock.Scripts.DOTS
{

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSimulationGroup))]
    public partial class PushBlockSystem : SystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<PushBlockAgentTagsComponent>();
        }

        protected override void OnUpdate()
        {


            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            Dependency = new CountNumCollisionEvents
            {
                ECB = ecb,
                Blocks = GetComponentLookup<PushBlockBlockTagsComponent>(),
                Goals = GetComponentLookup<PushBlockGoalTagsComponent>()
            }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(),Dependency);

            var agent = GameObject.FindWithTag("agent");
            var rayComponent = agent.GetComponent<RayPerceptionSensorComponentDOTS>();
            var agentComponent = agent.GetComponent<FakePushBlockAgent>();

            if(rayComponent == null) return;
            if(rayComponent.IsInit == false) return;

            PhysicsWorld world = SystemAPI.GetSingletonRW<PhysicsWorldSingleton>().ValueRW.PhysicsWorld;

            var rays = rayComponent.RaySensor;
            var rayOutputs = rays.RayPerceptionOutput;
            var rayInput = rays.RayPerceptionInput;

            EntityQuery query = SystemAPI.QueryBuilder().WithAll<BlockGoalCollisionSignal>().Build();

            if (!query.IsEmpty)
            {
                EntityManager.DestroyEntity(query);
                agentComponent.ScoredAGoal();
            }

            foreach (var (LT,PBA,PV,PM) in SystemAPI.Query<RefRW<LocalTransform>,RefRO<PushBlockAgentTagsComponent>,RefRW<PhysicsVelocity>,RefRO<PhysicsMass>>())
            {
                NativeArray<RaycastInput> inputs = new NativeArray<RaycastInput>(rayInput.Angles.Count, Allocator.Temp);
                for (int i = 0; i < rayInput.Angles.Count; i++)
                {
                    var input = inputs[i];
                    var temLT = LT.ValueRO;
                    input.Start = temLT.Position;
                    temLT = temLT.RotateY(Mathf.Deg2Rad * (rayInput.Angles[i] - 90));
                    input.End = temLT.Position + temLT.Forward() * rayInput.RayLength;
                    var filter = CollisionFilter.Default;
                    filter.CollidesWith = ~1u;
                    input.Filter = filter;
                    inputs[i] = input;

                    var hit = world.CastRay(input,out var raycastHit);
                    rayOutputs.RayOutputs[i] = raycastHit;
                    var tag = raycastHit.Material.CustomTags;

                    Color color = Color.black;
                    if(tag == 4) color = Color.green;
                    if(tag == 8) color = Color.red;

                    Debug.DrawRay(input.Start,raycastHit.Position - input.Start,color);

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
