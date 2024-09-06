using Unity.Entities;
using Unity.MLAgents;
using Unity.Physics.Systems;

namespace ML_Agents.Examples.PushBlock.Scripts.DOTS
{
    [UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
    public partial class PushBlockAcademyFixedUpdate : SystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<PushBlockAgentTagsComponent>();
        }

        protected override void OnUpdate()
        {
            Academy.Instance.EnvironmentStep();
        }
    }
}
