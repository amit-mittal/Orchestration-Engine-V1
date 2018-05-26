namespace TenantUpdateActor.Interfaces
{
    public enum JobState
    {
        Pending = 0,

        Begin,

        InProgress,

        Completed,

        Failed,

        Alerted
    }
}
