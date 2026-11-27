using Domain.Entities;

namespace Application.Interfaces;

public interface IMileageLogStore
{
    Task AddAsync(MileageLogEntry entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MileageLogEntry>> ListAsync(CancellationToken cancellationToken = default);
}
