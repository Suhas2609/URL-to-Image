using VehicleImage.Api.Exceptions;
using VehicleImage.Api.ImageProcessing;
using VehicleImage.Api.Models;
using VehicleImage.Api.Retrieval;
using VehicleImage.Api.Security;
using VehicleImage.Api.Storage;

namespace VehicleImage.Api.Services;

public sealed class ImageIngestionService : IImageIngestionService
{
    private readonly UrlValidator _urlValidator;
    private readonly RetrievalOrchestrator _orchestrator;
    private readonly ImageValidator _imageValidator;
    private readonly IImageStorage _imageStorage;
    private readonly ILogger<ImageIngestionService> _logger;

    public ImageIngestionService(
        UrlValidator urlValidator,
        RetrievalOrchestrator orchestrator,
        ImageValidator imageValidator,
        IImageStorage imageStorage,
        ILogger<ImageIngestionService> logger)
    {
        _urlValidator = urlValidator;
        _orchestrator = orchestrator;
        _imageValidator = imageValidator;
        _imageStorage = imageStorage;
        _logger = logger;
    }

    public async Task<IngestedImage> IngestAsync(
        string imageUrl,
        CancellationToken cancellationToken = default)
    {
        Uri validatedSourceUri = _urlValidator.Validate(imageUrl);

        ImageIngestionException? lastValidationFailure = null;
        int candidateNumber = 0;

        await foreach (var retrieved in _orchestrator.RetrieveCandidatesAsync(validatedSourceUri, cancellationToken))
        {
            candidateNumber++;
            await using (retrieved)
            {
                ImageMetadata metadata;
                try
                {
                    metadata = await _imageValidator.ValidateAsync(retrieved.Content, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (ImageIngestionException ex)
                {
                    lastValidationFailure = ex;
                    _logger.LogInformation(
                        "Candidate {CandidateNumber} from {FinalSourceUrl} (strategy={Strategy}) failed image validation [{Code}]: {Message}",
                        candidateNumber,
                        retrieved.FinalSourceUrl,
                        retrieved.Strategy,
                        ex.Code,
                        ex.Message);
                    continue;
                }

                string storageKey = await _imageStorage.SaveAsync(
                    retrieved.Content,
                    metadata.FileExtension,
                    cancellationToken);

                string formatString = metadata.Format switch
                {
                    ImageFormatKind.Jpeg => "JPEG",
                    ImageFormatKind.Png => "PNG",
                    ImageFormatKind.WebP => "WebP",
                    _ => metadata.Format.ToString().ToUpperInvariant()
                };

                return new IngestedImage(
                    ImageId: Guid.NewGuid(),
                    StorageKey: storageKey,
                    ContentType: metadata.ContentType,
                    Format: formatString,
                    FileSize: metadata.FileSize,
                    Width: metadata.Width,
                    Height: metadata.Height,
                    Sha256: metadata.Sha256,
                    SourceUrl: validatedSourceUri.AbsoluteUri,
                    FinalSourceUrl: retrieved.FinalSourceUrl.AbsoluteUri,
                    RetrievalStrategy: retrieved.Strategy);
            }
        }

        if (lastValidationFailure is not null)
        {
            throw lastValidationFailure;
        }

        throw ImageIngestionException.NoImageFound();
    }
}
