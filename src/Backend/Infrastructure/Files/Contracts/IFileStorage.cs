namespace Infrastructure.Files.Contracts;

public interface IFileStorage
{
    Task SaveAsync(string storagePath, Stream content, CancellationToken cancellationToken);
    Task<Stream?> OpenReadAsync(string storagePath, CancellationToken cancellationToken);
    Task DeleteAsync(string storagePath, CancellationToken cancellationToken);
}
