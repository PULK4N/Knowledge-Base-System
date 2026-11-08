using Microsoft.EntityFrameworkCore;
using PolicyModule.Persistence.Models;

namespace PolicyModule.Persistence.Tests;

internal sealed class TestPolicyDbContext(
    DbContextOptions<TestPolicyDbContext> options
) : DbContext(options), IPolicyModuleDbContext
{
    public DbSet<GeneralPolicyText> GeneralPolicyTexts =>
        Set<GeneralPolicyText>();
    public DbSet<TopicPolicyText> TopicPolicyTexts =>
        Set<TopicPolicyText>();
    public DbSet<AgentFamilyPolicyText> AgentFamilyPolicyTexts =>
        Set<AgentFamilyPolicyText>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<GeneralPolicyText>()
            .HasKey(text => text.Id);
        modelBuilder
            .Entity<TopicPolicyText>()
            .HasKey(text => text.Id);
        modelBuilder
            .Entity<AgentFamilyPolicyText>()
            .HasKey(text => text.Id);
    }
}
