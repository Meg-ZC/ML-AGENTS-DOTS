using Unity.Entities;
using UnityEngine;

namespace ML_Agents.Examples._3DBall.Scripts.Ball3DAgentDOTS
{
    public class Test : MonoBehaviour
    {
        private class TestBaker : Baker<Test>
        {
            public override void Bake(Test authoring)
            {
                var com = new Ball3DComponent();
                var e = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(e, com);

            }
        }
    }
}

