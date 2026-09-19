using CSharpFunctionalExtensions;
using Shared.SharedKernel;

namespace FileService.VideoProcessing.Pipeline;

public sealed class ProcessingResult
{
    private readonly Result<ProcessingContext, Error> _result;

    private ProcessingResult(Result<ProcessingContext, Error> result, bool isCritical, string? resultData)
    {
        _result = result;
        IsCritical = isCritical;
        ResultData = resultData;
    }

    public bool IsSuccess => _result.IsSuccess;

    public bool IsFailure => _result.IsFailure;

    public ProcessingContext Context => _result.Value;

    public Error Error => _result.Error;

    public bool IsCritical { get; }

    public string? ResultData { get; }

    public static ProcessingResult Success(ProcessingContext context, string? resultData = null) =>
        new(context, false, resultData);

    public static ProcessingResult Failure(Error error, bool isCritical = true) =>
        new(error, isCritical, null);
}
