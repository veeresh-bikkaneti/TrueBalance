using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public sealed class EfBookSettingsStore(OfflineCPADbContext db) : IBookSettingsStore
{
    public Task<BookSettings?> GetAsync(CancellationToken cancellationToken = default) =>
        db.BookSettings.SingleOrDefaultAsync(cancellationToken);
}
