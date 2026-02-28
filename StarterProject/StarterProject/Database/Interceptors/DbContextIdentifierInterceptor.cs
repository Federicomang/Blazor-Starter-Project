using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using StarterProject.Database.Entities;

namespace StarterProject.Database.Interceptors
{
    public class DbContextIdentifierInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (eventData.Context is not null)
                CheckIdentifiers(eventData.Context).Wait();

            return result;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context is not null)
                await CheckIdentifiers(eventData.Context, cancellationToken);

            return result;
        }

        private static async Task CheckIdentifiers(DbContext dbContext, CancellationToken cancellationToken = default)
        {
            dbContext.ChangeTracker.DetectChanges();

            var entries = dbContext.ChangeTracker.Entries().ToList();

            foreach (var entry in entries)
            {
                if (entry.Entity is IAuthEntity authEntity)
                {
                    if (entry.State == EntityState.Added)
                    {
                        dbContext.Set<Identifier>().Add(new()
                        {
                            IdentifierKey = authEntity.AuthIdentifier,
                            IdentifierId = authEntity.Id!
                        });
                    }
                    else if (entry.State == EntityState.Deleted)
                    {
                        var entity = await dbContext.Set<Identifier>()
                            .FirstOrDefaultAsync(x => x.IdentifierKey == authEntity.AuthIdentifier && x.Id == authEntity.Id, cancellationToken);
                        if (entity != null)
                        {
                            dbContext.Set<Identifier>().Remove(entity);
                        }
                    }
                }
            }
        }
    }
}
