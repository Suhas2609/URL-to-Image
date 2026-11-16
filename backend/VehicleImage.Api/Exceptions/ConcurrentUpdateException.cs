namespace VehicleImage.Api.Exceptions;

public sealed class ConcurrentUpdateException : Exception
{
    public string Code => ErrorCodes.ConcurrentUpdate;

    public ConcurrentUpdateException(string message = "The vehicle image was modified by another request. Please retry.")
        : base(message)
    {
    }
}
