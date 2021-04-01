namespace Alachisoft.NGroups.Protocols.pbcast
{
    internal enum JoinResult
    {
        Success,
        MaxMbrLimitReached, //muds: in express edition only 3 members can join the cluster.
        Rejected,
        HandleLeaveInProgress,
        HandleJoinInProgress,
        MembershipChangeAlreadyInProgress
    }
}