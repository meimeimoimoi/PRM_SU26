namespace SmartDine.Domain.Exceptions;

public class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityName, Guid id)
        : base($"{entityName} with Id '{id}' was not found.") { }

    public EntityNotFoundException(string message) : base(message) { }
}
