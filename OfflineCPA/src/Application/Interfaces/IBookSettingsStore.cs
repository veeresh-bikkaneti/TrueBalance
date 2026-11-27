using Domain.Entities;

namespace Application.Interfaces;

public interface IBookSettingsStore
{
    Task<BookSettings?> GetAsync(CancellationToken cancellationToken = default);
}
