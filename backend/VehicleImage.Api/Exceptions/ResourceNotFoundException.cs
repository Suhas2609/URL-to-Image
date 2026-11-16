namespace VehicleImage.Api.Exceptions;

public sealed class ResourceNotFoundException : Exception
{
    public string Code { get; }

    public ResourceNotFoundException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public static ResourceNotFoundException Vehicle(Guid vehicleId)
        => new(ErrorCodes.VehicleNotFound, $"Vehicle '{vehicleId}' was not found.");

    public static ResourceNotFoundException Image(Guid imageId)
        => new(ErrorCodes.ImageNotFound, $"Image '{imageId}' was not found.");
}
