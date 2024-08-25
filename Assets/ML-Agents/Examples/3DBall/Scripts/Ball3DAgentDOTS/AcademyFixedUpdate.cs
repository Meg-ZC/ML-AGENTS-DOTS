using Unity.Entities;
using Unity.MLAgents;
using Unity.Physics.Systems;

namespace ML_Agents.Examples._3DBall.Scripts.Ball3DAgentDOTS
{
    [UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
    public partial class AcademyFixedUpdate : SystemBase
    {
        protected override void OnUpdate()
        {
            Academy.Instance.EnvironmentStep();
        }
    }
}
