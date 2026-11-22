using IdentityModule.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace IdentityModule.Persistence;

public interface IIdentityModuleDbContext
{
    DbSet<AccountListEntry> AccountListEntries { get; }
}
