using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using StarterProject.Attributes.ExtendedAudit;
using StarterProject.Database.Entities;
using StarterProject.Extensions;
using System.Reflection;

namespace StarterProject.Database
{
    public class DbContextSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
    {
        private readonly List<(string TempToken, EntityEntry Entry)> _pendingEntityIdFixups = [];

        private static readonly IEnumerable<string> IgnoredProperties = typeof(IAuditableEntity).GetProperties().Select(x => x.Name);

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            if (eventData.Context is not null)
                AddAuditEntries(eventData.Context);

            return result;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is not null)
                AddAuditEntries(eventData.Context);

            return ValueTask.FromResult(result);
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            if (eventData.Context is not null)
                FixupInsertedEntityIds(eventData.Context);

            return result;
        }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is not null)
                await FixupInsertedEntityIdsAsync(eventData.Context, cancellationToken);

            return result;
        }

        private void AddAuditEntries(DbContext context)
        {
            // Evita loop: se stai già salvando audit, non auditare
            //if (context.ChangeTracker.Entries().Any(e => e.Entity is AuditLog or AuditLogDetail))
            //    return;

            context.ChangeTracker.DetectChanges();

            var utcNow = DateTime.UtcNow;

            var entries = context.ChangeTracker.Entries()
                .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .ToList();

            var (table, currentId) = GetCurrentIdentifier();

            foreach (var entry in entries)
            {
                var isAuditableEntity = false;

                if (entry.Entity is AuditLog or AuditLogDetail)
                    continue;

                if (entry.Entity is IAuditableEntity entity)
                {
                    isAuditableEntity = true;
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            entity.CreatedOn = utcNow;
                            entity.CreatedBy = currentId!;
                            entity.TableThatCreated = table!;
                            break;

                        case EntityState.Modified:
                            entity.CreatedOn = entry.OriginalValues.GetValue<DateTime>(nameof(IAuditableEntity.CreatedOn));
                            entity.CreatedBy = entry.OriginalValues.GetValue<string>(nameof(IAuditableEntity.CreatedBy));
                            entity.TableThatCreated = entry.OriginalValues.GetValue<string>(nameof(IAuditableEntity.TableThatCreated));
                            entity.LastModifiedOn = utcNow;
                            entity.LastModifiedBy = currentId;
                            entity.TableThatModified = table;
                            break;
                    }
                }

                var entityName = entry.Metadata.ClrType.Name;

                var auditAttr = entry.Metadata.ClrType.GetCustomAttribute<AuditAttribute>();

                if (auditAttr == null) continue;

                var pkProp = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
                if (pkProp is null) continue;

                var action = entry.State switch
                {
                    EntityState.Added => auditAttr.TrackType.HasFlag(AuditTrackType.Added) ? "INSERT" : null,
                    EntityState.Deleted => auditAttr.TrackType.HasFlag(AuditTrackType.Deleted) ? "DELETE" : null,
                    _ => auditAttr.TrackType.HasFlag(AuditTrackType.Modified) ? "UPDATE" : null
                };

                if (string.IsNullOrEmpty(action)) continue;

                var entityId = GetEntityIdString(pkProp, out var tempToken);
                if (tempToken is not null)
                    _pendingEntityIdFixups.Add((tempToken, entry));

                var auditLog = new AuditLog
                {
                    EntityName = entityName,
                    EntityId = entityId,
                    Action = action,
                    ChangedOn = utcNow,
                    ChangedBy = currentId,
                    TableThatChanged = table
                };

                var isEdited = entry.State == EntityState.Modified;

                // Dettagli solo per UPDATE
                if (isEdited || entry.State == EntityState.Deleted)
                {
                    var includeAllProps = auditAttr.IncludeAllProperties;
                    foreach (var prop in entry.Properties)
                    {
                        if (!prop.IsModified && prop.Metadata.IsPrimaryKey()) continue;

                        var propAttr = prop.Metadata.ClrType.GetCustomAttribute<AuditPropAttribute>();
                        if (propAttr?.Exclude == true && (!includeAllProps || propAttr != null)) continue;

                        var propName = prop.Metadata.Name;
                        if (isAuditableEntity && IgnoredProperties.Contains(propName)) continue;

                        var oldVal = prop.OriginalValue;
                        object? newVal = null;

                        if (isEdited)
                        {
                            newVal = prop.CurrentValue;
                            if (Equals(oldVal, newVal)) continue;
                        }

                        auditLog.Details.Add(new AuditLogDetail
                        {
                            FieldName = propName,
                            OldValue = ToAuditString(oldVal),
                            NewValue = ToAuditString(newVal)
                        });
                    }
                }

                context.Set<AuditLog>().Add(auditLog);
            }
        }

        private (string? table, string? entityId) GetCurrentIdentifier()
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var httpContextItems = httpContext.GetItems();
                if (httpContextItems.AuthenticationScheme == IdentityConstants.ApplicationScheme)
                {
                    var applicationId = httpContext.GetItems().ApplicationId;
                    return (ApplicationDbContext.AUDIT_TABLE_APPLICATION, applicationId);
                }
                else
                {
                    var user = httpContext.GetItems().User;
                    return (ApplicationDbContext.AUDIT_TABLE_USER, user?.Id);
                }
            }
            return (null, null);
        }

        private static string GetEntityIdString(PropertyEntry pkProp, out string? tempToken)
        {
            tempToken = null;

            var value = pkProp.CurrentValue;

            if (pkProp.IsTemporary || value is null)
            {
                tempToken = $"__temp__:{Guid.NewGuid():N}";
                return tempToken;
            }

            return Convert.ToString(value) ?? string.Empty;
        }

        private void FixupInsertedEntityIds(DbContext context)
        {
            if (_pendingEntityIdFixups.Count == 0) return;

            foreach (var (tempToken, entry) in _pendingEntityIdFixups.ToList())
            {
                var pk = entry.Properties.First(p => p.Metadata.IsPrimaryKey());
                var realId = Convert.ToString(pk.CurrentValue);

                if (string.IsNullOrWhiteSpace(realId))
                    continue;

                context.Database.ExecuteSqlRaw(
                    @"UPDATE History.AuditLog SET EntityId = {0} WHERE EntityId = {1};",
                    realId, tempToken);

                _pendingEntityIdFixups.Remove((tempToken, entry));
            }
        }

        private async Task FixupInsertedEntityIdsAsync(DbContext context, CancellationToken ct)
        {
            if (_pendingEntityIdFixups.Count == 0) return;

            foreach (var (tempToken, entry) in _pendingEntityIdFixups.ToList())
            {
                var pk = entry.Properties.First(p => p.Metadata.IsPrimaryKey());
                var realId = Convert.ToString(pk.CurrentValue);

                if (string.IsNullOrWhiteSpace(realId))
                    continue;

                await context.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE History.AuditLog SET EntityId = {realId} WHERE EntityId = {tempToken};",
                    ct);

                _pendingEntityIdFixups.Remove((tempToken, entry));
            }
        }

        private static string? ToAuditString(object? value)
        {
            if (value is null) return null;

            return value switch
            {
                DateTime dt => dt.ToUniversalTime().ToString("O"),
                DateTimeOffset dto => dto.ToUniversalTime().ToString("O"),
                decimal dec => dec.ToString(System.Globalization.CultureInfo.InvariantCulture),
                double dbl => dbl.ToString(System.Globalization.CultureInfo.InvariantCulture),
                float fl => fl.ToString(System.Globalization.CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
        }
    }
}
