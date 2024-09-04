using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.MLAgents;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace ML_Agents.Examples.PushBlock.Scripts.DOTS
{

    [UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
    public partial class PushBlockSystem : SystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<PushBlockAgentTagsComponent>();
        }

        protected override void OnUpdate()
        {
            var agent = GameObject.FindWithTag("agent");
            var rayComponent = agent.GetComponent<RayPerceptionSensorComponentDOTS>();

            if(rayComponent == null) return;
            if(rayComponent.IsInit == false) return;

            PhysicsWorld world = SystemAPI.GetSingletonRW<PhysicsWorldSingleton>().ValueRW.PhysicsWorld;

            var rays = rayComponent.RaySensor;
            var rayOutputs = rays.RayPerceptionOutput;
            var rayInput = rays.RayPerceptionInput;

            foreach (var (LT,PBA) in SystemAPI.Query<RefRO<LocalTransform>,RefRO<PushBlockAgentTagsComponent>>())
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
            }
        }
    }
}
