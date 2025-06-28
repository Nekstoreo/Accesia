namespace Accesia.Application.Common.Exceptions;

public class BusinessRuleException : Exception
{
    public BusinessRuleException(string ruleName, string context, string message)
        : base(message)
    {
        RuleName = ruleName;
        Context = context;
    }

    public BusinessRuleException(string ruleName, string context, string message, Exception innerException)
        : base(message, innerException)
    {
        RuleName = ruleName;
        Context = context;
    }

    public string RuleName { get; }
    public string Context { get; }
}

public class InsufficientPrivilegesException : BusinessRuleException
{
    public InsufficientPrivilegesException(string userId, string requiredRole, string action)
        : base("InsufficientPrivileges", $"User:{userId}",
            $"Se requiere el rol '{requiredRole}' para realizar la acción: {action}")
    {
        RequiredRole = requiredRole;
        UserId = userId;
    }

    public string RequiredRole { get; }
    public string UserId { get; }
}

public class CannotPerformActionException : BusinessRuleException
{
    public CannotPerformActionException(string userId, string actionName, string reason)
        : base("CannotPerformAction", $"User:{userId}", $"No se puede realizar la acción '{actionName}': {reason}")
    {
        UserId = userId;
        ActionName = actionName;
    }

    public string UserId { get; }
    public string ActionName { get; }
}