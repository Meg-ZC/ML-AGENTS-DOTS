using Unity.Entities;
using Unity.MLAgents.Sensors;

namespace ML_Agents.DOTS_Scripts
{
    public struct DOTSSensorComponent : IComponentData,ISensor
    {
        public ObservationSpec GetObservationSpec()
        {
            throw new System.NotImplementedException();
        }

        public int Write(ObservationWriter writer)
        {
            throw new System.NotImplementedException();
        }

        public byte[] GetCompressedObservation()
        {
            throw new System.NotImplementedException();
        }

        public void Update()
        {
            throw new System.NotImplementedException();
        }

        public void Reset()
        {
            throw new System.NotImplementedException();
        }

        public CompressionSpec GetCompressionSpec()
        {
            throw new System.NotImplementedException();
        }

        public string GetName()
        {
            throw new System.NotImplementedException();
        }
    }
}
