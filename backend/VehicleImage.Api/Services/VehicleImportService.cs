using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Options;
using VehicleImage.Api.Configuration;
using VehicleImage.Api.DTOs.Vehicles;
using VehicleImage.Api.Exceptions;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace VehicleImage.Api.Services;

/// <summary>
/// Imports vehicles from a CSV file. Parsing and row validation happen up front; each valid row
/// is then created sequentially through <see cref="IVehicleService.CreateAsync"/>, which already
/// owns image ingestion (SSRF protection, retrieval, validation, storage), persistence and the
/// compensating file delete on persistence failure.
/// </summary>
public sealed class VehicleImportService : IVehicleImportService
{
    private const string MakeColumn = "make";
    private const string ModelColumn = "model";
    private const string VariantColumn = "variant";
    private const string YearColumn = "year";
    private const string ImageUrlColumn = "imageurl";

    private static readonly string[] RequiredColumns = [MakeColumn, ModelColumn, YearColumn, ImageUrlColumn];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BulkImportOptions _options;
    private readonly ILogger<VehicleImportService> _logger;

    public VehicleImportService(
        IServiceScopeFactory scopeFactory,
        IOptions<BulkImportOptions> options,
        ILogger<VehicleImportService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<VehicleImportResponse> ImportCsvAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        ValidateFile(file);

        // 1. Parse and validate the whole file before any network or database work, so a malformed
        //    or oversized CSV is rejected without partially importing it.
        List<CsvRow> rows;
        await using (Stream stream = file.OpenReadStream())
        {
            rows = await ParseAsync(stream, cancellationToken);
        }

        _logger.LogInformation(
            "Starting CSV vehicle import of {RecordCount} record(s) from {FileName}.",
            rows.Count,
            file.FileName);

        // 2. Process rows sequentially. One failed row never stops the remaining rows.
        var results = new List<VehicleImportRowResult>(rows.Count);
        foreach (CsvRow row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(await ProcessRowAsync(row, cancellationToken));
        }

        int successfulRows = results.Count(r => r.Success);

        _logger.LogInformation(
            "CSV vehicle import from {FileName} finished: {SuccessfulRows} succeeded, {FailedRows} failed.",
            file.FileName,
            successfulRows,
            results.Count - successfulRows);

        return new VehicleImportResponse(
            TotalRows: results.Count,
            SuccessfulRows: successfulRows,
            FailedRows: results.Count - successfulRows,
            Results: results);
    }

    private void ValidateFile(IFormFile file)
    {
        if (file.Length == 0)
        {
            throw new ValidationException("The uploaded CSV file is empty.");
        }

        if (file.Length > _options.MaxFileBytes)
        {
            throw new ValidationException(
                $"The uploaded CSV file exceeds the maximum allowed size of {_options.MaxFileBytes / 1024} KB.");
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("Only .csv files are accepted.");
        }
    }

    private async Task<List<CsvRow>> ParseAsync(Stream stream, CancellationToken cancellationToken)
    {
        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim,
            DetectDelimiter = false
        };

        using var reader = new StreamReader(stream, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: true);
        using var parser = new CsvParser(reader, configuration);

        var rows = new List<CsvRow>();
        int currentRow = 0;

        try
        {
            if (!await parser.ReadAsync())
            {
                throw new ValidationException("The CSV file does not contain a header row.");
            }

            Dictionary<string, int> columns = BuildColumnMap(parser.Record ?? []);
            int headerFieldCount = parser.Record?.Length ?? 0;

            while (await parser.ReadAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();
                currentRow = parser.Row;
                string[] record = parser.Record ?? [];

                // Skip rows that only contain separators/whitespace (e.g. ",,,,").
                if (record.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                if (rows.Count >= _options.MaxRecords)
                {
                    throw new ValidationException(
                        $"The CSV file contains more than the maximum of {_options.MaxRecords} vehicle records per import.");
                }

                rows.Add(BuildRow(parser.Row, record, headerFieldCount, columns));
            }
        }
        catch (CsvHelperException ex)
        {
            _logger.LogInformation(ex, "Rejected malformed CSV upload near row {Row}.", currentRow);
            throw new ValidationException(
                currentRow > 0
                    ? $"The CSV file is malformed near row {currentRow} (for example an unterminated quoted field)."
                    : "The CSV file is malformed and could not be parsed.");
        }

        if (rows.Count == 0)
        {
            throw new ValidationException("The CSV file does not contain any vehicle records.");
        }

        return rows;
    }

    private static Dictionary<string, int> BuildColumnMap(string[] header)
    {
        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < header.Length; i++)
        {
            string name = (header[i] ?? string.Empty).Trim().Replace(" ", string.Empty);
            if (name.Length == 0)
            {
                continue;
            }

            if (!columns.TryAdd(name, i))
            {
                throw new ValidationException($"The CSV header contains the duplicate column '{header[i].Trim()}'.");
            }
        }

        string[] missing = RequiredColumns.Where(c => !columns.ContainsKey(c)).ToArray();
        if (missing.Length > 0)
        {
            throw new ValidationException(
                $"The CSV header is missing required column(s): {string.Join(", ", missing.Select(DisplayName))}. " +
                "Required columns: Make, Model, Year, ImageUrl (Variant is optional).");
        }

        return columns;
    }

    private static CsvRow BuildRow(int rowNumber, string[] record, int headerFieldCount, Dictionary<string, int> columns)
    {
        string? Field(string column) =>
            columns.TryGetValue(column, out int index) && index < record.Length
                ? NullIfWhiteSpace(record[index])
                : null;

        string? rawYear = Field(YearColumn);
        int? parsedYear = int.TryParse(rawYear, NumberStyles.Integer, CultureInfo.InvariantCulture, out int yearValue)
            ? yearValue
            : null;

        var row = new CsvRow(
            RowNumber: rowNumber,
            Make: Field(MakeColumn),
            Model: Field(ModelColumn),
            Variant: Field(VariantColumn),
            Year: parsedYear,
            ImageUrl: Field(ImageUrlColumn));

        if (record.Length != headerFieldCount)
        {
            return row with
            {
                ValidationError = $"Row has {record.Length} field(s) but the header defines {headerFieldCount} column(s)."
            };
        }

        if (!parsedYear.HasValue)
        {
            return row with
            {
                ValidationError = rawYear is null
                    ? "Year is required."
                    : $"Year '{rawYear}' is not a valid whole number."
            };
        }

        // Reuse the single-vehicle request DTO and its DataAnnotations/IValidatableObject rules
        // (required fields, lengths, year range) so CSV rows obey exactly the same validation.
        var request = new CreateVehicleRequest
        {
            Make = row.Make ?? string.Empty,
            Model = row.Model ?? string.Empty,
            Variant = row.Variant,
            Year = parsedYear.Value,
            ImageUrl = row.ImageUrl ?? string.Empty
        };

        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true))
        {
            return row with
            {
                ValidationError = string.Join(" ", validationResults
                    .Select(r => r.ErrorMessage)
                    .Where(m => !string.IsNullOrWhiteSpace(m)))
            };
        }

        return row with { Request = request };
    }

    private async Task<VehicleImportRowResult> ProcessRowAsync(CsvRow row, CancellationToken cancellationToken)
    {
        if (row.Request is null)
        {
            return Failed(row, ErrorCodes.ValidationFailed, row.ValidationError ?? "The record is invalid.");
        }

        // A fresh scope (and therefore a fresh DbContext) per row: if one row's SaveChanges fails,
        // its unsaved entities must not stay tracked and be re-inserted with the next row.
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        var vehicleService = scope.ServiceProvider.GetRequiredService<IVehicleService>();

        try
        {
            ImageSubmissionResponse created = await vehicleService.CreateAsync(row.Request, cancellationToken);

            return new VehicleImportRowResult(
                RowNumber: row.RowNumber,
                Make: row.Make,
                Model: row.Model,
                Year: row.Year,
                Success: true,
                VehicleId: created.VehicleId,
                ImageId: created.ImageId,
                ImageUrl: created.ImageUrl,
                ErrorCode: null,
                ErrorMessage: null);
        }
        catch (ImageIngestionException ex)
        {
            // Expected, client-safe failures (invalid/unsafe URL, HTTP errors, invalid image,
            // storage or database failure). VehicleService has already removed any stored file.
            _logger.LogInformation(
                "CSV import row {Row} failed with [{Code}]: {Message}",
                row.RowNumber,
                ex.Code,
                ex.Message);

            return Failed(row, ex.Code, ex.Message);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected failure while importing CSV row {Row}; aborting the import.", row.RowNumber);
            throw;
        }
    }

    private static VehicleImportRowResult Failed(CsvRow row, string code, string errorMessage) =>
        new(
            RowNumber: row.RowNumber,
            Make: row.Make,
            Model: row.Model,
            Year: row.Year,
            Success: false,
            VehicleId: null,
            ImageId: null,
            ImageUrl: null,
            ErrorCode: code,
            ErrorMessage: errorMessage);

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string DisplayName(string column) => column switch
    {
        MakeColumn => "Make",
        ModelColumn => "Model",
        YearColumn => "Year",
        ImageUrlColumn => "ImageUrl",
        _ => column
    };

    private sealed record CsvRow(
        int RowNumber,
        string? Make,
        string? Model,
        string? Variant,
        int? Year,
        string? ImageUrl)
    {
        public CreateVehicleRequest? Request { get; init; }
        public string? ValidationError { get; init; }
    }
}
