using Unity.Entities;
using UnityEngine;

namespace ML_Agents.Examples.PushBlock.Scripts.DOTS
{
    public class TagAuthoring : MonoBehaviour
    {
        public enum TagType
        {
            Agent,
            Block,
            Goal
        }

        public TagType tagType;
        private class TagAuthoringBaker : Baker<TagAuthoring>
        {
            public override void Bake(TagAuthoring authoring)
            {
                var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                switch (authoring.tagType)
                {
                    case TagType.Agent:
                        AddComponent<PushBlockAgentTagsComponent>(e);
                        break;
                    case TagType.Block:
                        AddComponent<PushBlockBlockTagsComponent>(e);
                        break;
                    case TagType.Goal:
                        AddComponent<PushBlockGoalTagsComponent>(e);
                        break;
                }
            }
        }
    }
}

