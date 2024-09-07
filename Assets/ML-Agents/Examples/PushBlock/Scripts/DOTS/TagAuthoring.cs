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
            Goal,
            Area
        }

        public GameObject block;

        public TagType tagType;
        public TransformUsageFlags transformUsageFlags;
        private class TagAuthoringBaker : Baker<TagAuthoring>
        {
            public override void Bake(TagAuthoring authoring)
            {
                var e = GetEntity(authoring.gameObject, authoring.transformUsageFlags);
                switch (authoring.tagType)
                {
                    case TagType.Agent:
                        AddComponent(e,new PushBlockAgentTagsComponent());
                        break;
                    case TagType.Block:
                        AddComponent<PushBlockBlockTagsComponent>(e);
                        break;
                    case TagType.Area:
                        AddComponent<PushBlockAreaTagsComponent>(e);
                        break;
                    case TagType.Goal:
                        AddComponent<PushBlockGoalTagsComponent>(e);
                        break;
                }
            }
        }
    }
}

