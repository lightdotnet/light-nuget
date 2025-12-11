namespace Light.Domain.Entities.Interfaces;

public interface IHasAuditTime : IHasCreationTime, IHasModificationTime
{ }

public interface IHasCreationTime
{
    DateTimeOffset Created { get; set; }
}

public interface IHasModificationTime
{
    DateTimeOffset? LastModified { get; set; }
}

