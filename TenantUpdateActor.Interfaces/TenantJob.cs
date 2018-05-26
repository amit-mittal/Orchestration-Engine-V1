using System.Runtime.Serialization;

namespace TenantUpdateActor.Interfaces
{
    [DataContract]
    public class TenantJob
    {
        [DataMember]
        public string JobId;

        [DataMember]
        public string RolloutId;

        [DataMember]
        public string TenantId;

        [DataMember]
        public string WorkflowName;

        [DataMember]
        public JobState JobStatus;
    }
}