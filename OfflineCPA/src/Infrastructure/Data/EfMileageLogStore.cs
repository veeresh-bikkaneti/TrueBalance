using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public sealed class EfMileageLogStore(OfflineCPADbContext db) : IMileageLogStore
{
    public async Task AddAsync(MileageLogEntry entry, CancellationToken cancellationToken = default)
    {
        db.MileageLog.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MileageLogEntry>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.MileageLog.OrderBy(e => e.Date).ThenBy(e => e.StartOdometer).ToListAsync(cancellationToken);
}
