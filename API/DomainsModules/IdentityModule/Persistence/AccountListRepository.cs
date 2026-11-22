using System.Data;
using IdentityModule.Application.DTOs;
using IdentityModule.Application.Persistence;
using IdentityModule.Domain;
using IdentityModule.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace IdentityModule.Persistence;

public sealed class AccountListRepository(IIdentityModuleDbContext dbContext) : IAccountListRepository
{
    public Task<List<AgentDto>> ListAgents(CancellationToken cancellationToken = default) =>
        dbContext.AccountListEntries
            .AsNoTracking()
            .Where(entry => entry.IsAgent)
            .OrderByDescending(entry => entry.UpdatedAt)
            .ThenBy(entry => entry.UserName)
            .Select(entry => new AgentDto(entry.AccountId, entry.UserName, entry.DisplayName,
                entry.IsAdministrator, entry.Permissions))
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Replaces the row of every account received with its latest state, so replaying the same
    /// events gives the same rows. An older state never overwrites a newer one.
    /// </summary>
    internal async Task Write(List<AccountListUpdate> updates, CancellationToken cancellationToken = default)
    {
        if (updates.Count == 0)
            return;

        var context = dbContext as DbContext
            ?? throw new InvalidOperationException(
                $"{nameof(IIdentityModuleDbContext)} must be implemented by a {nameof(DbContext)}.");
        await using var transaction = context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        var latestUpdates = updates
            .GroupBy(update => update.Account.Id.Value)
            .Select(group => group.MaxBy(update => update.OrderNumber)!)
            .ToList();
        var accountIds = latestUpdates.Select(update => update.Account.Id.Value).ToList();
        var current = await dbContext.AccountListEntries
            .Where(entry => accountIds.Contains(entry.AccountId))
            .ToDictionaryAsync(entry => entry.AccountId, cancellationToken);

        foreach (var update in latestUpdates)
        {
            if (current.TryGetValue(update.Account.Id.Value, out var existing))
            {
                if (existing.ProjectedOrderNumber > update.OrderNumber)
                    continue;
                dbContext.AccountListEntries.Remove(existing);
            }

            if (!update.Account.IsDeleted)
                await dbContext.AccountListEntries.AddAsync(ToEntry(update), cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);

        foreach (var trackedEntry in context.ChangeTracker.Entries<AccountListEntry>().ToList())
            trackedEntry.State = EntityState.Detached;
    }

    private static AccountListEntry ToEntry(AccountListUpdate update) => new()
    {
        AccountId = update.Account.Id.Value,
        UserName = update.Account.UserName,
        DisplayName = update.Account.DisplayName,
        IsAdministrator = update.Account.IsAdministrator,
        IsAgent = update.Account.IsAgent,
        Permissions = update.Account.Permissions.ToList(),
        UpdatedAt = update.UpdatedAt,
        ProjectedOrderNumber = update.OrderNumber
    };
}

internal sealed record AccountListUpdate(AccountStateData Account, long OrderNumber, DateTime UpdatedAt);
