using Unity.Entities;

namespace ML_Agents.Examples.PushBlock.Scripts.DOTS
{
    public struct PushBlockAgentTagsComponent : IComponentData
    {
        public Entity Block;
    }
    public struct PushBlockBlockTagsComponent : IComponentData
    {

    }
    public struct PushBlockGoalTagsComponent : IComponentData
    {

    }

    public struct BlockGoalCollisionSignal : IComponentData
    {

    }
}
