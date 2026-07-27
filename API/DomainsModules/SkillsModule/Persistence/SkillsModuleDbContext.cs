using Microsoft.EntityFrameworkCore;
using SkillsModule.Persistence.Models;

namespace SkillsModule.Persistence;

public sealed class SkillsModuleDbContext(
    DbContextOptions<SkillsModuleDbContext> options
) : DbContext(options)
{
    public DbSet<AttachmentContent> AttachmentContents =>
        Set<AttachmentContent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AttachmentContent>(
            attachment =>
            {
                attachment.HasKey(content => content.FileId);
                attachment
                    .Property(content => content.FileId)
                    .ValueGeneratedNever();
                attachment
                    .Property(content => content.Content)
                    .IsRequired();
            }
        );
    }
}
