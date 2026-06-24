namespace Application.Reviews.Models;

public enum ReviewOperationStatus
{
    Success,
    NotFound,
    Expired,
    Cancelled,
    InvalidState,
    Forbidden
}

public sealed class ReviewOperationResult<T>
{
    public ReviewOperationStatus Status { get; private set; }
    public T? Data { get; private set; }
    public string? ErrorMessage { get; private set; }

    private ReviewOperationResult(ReviewOperationStatus status, T? data = default, string? errorMessage = null)
    {
        Status = status;
        Data = data;
        ErrorMessage = errorMessage;
    }

    public static ReviewOperationResult<T> Success(T data) => new(ReviewOperationStatus.Success, data);
    public static ReviewOperationResult<T> NotFound() => new(ReviewOperationStatus.NotFound);
    public static ReviewOperationResult<T> Expired() => new(ReviewOperationStatus.Expired, errorMessage: "Review deadline has passed");
    public static ReviewOperationResult<T> Cancelled() => new(ReviewOperationStatus.Cancelled, errorMessage: "Review has been cancelled");
    public static ReviewOperationResult<T> InvalidState(string message) => new(ReviewOperationStatus.InvalidState, errorMessage: message);
    public static ReviewOperationResult<T> Forbidden() => new(ReviewOperationStatus.Forbidden, errorMessage: "Access denied");
}
