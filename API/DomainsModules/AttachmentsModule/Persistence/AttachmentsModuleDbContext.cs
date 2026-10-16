using AttachmentsModule.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace AttachmentsModule.Persistence;

public sealed class AttachmentsModuleDbContext(
    DbContextOptions<AttachmentsModuleDbContext> options
) : DbContext(options)
{
    /// <summary>
    /// Content type served for rows stored before attachments recorded their
    /// own type.
    /// </summary>
    public const string UnknownContentType = "application/octet-stream";

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
                    .Property(content => content.Name)
                    .IsRequired()
                    .HasDefaultValue(string.Empty);
                attachment
                    .Property(content => content.ContentType)
                    .IsRequired()
                    .HasDefaultValue(UnknownContentType);
                attachment
                    .Property(content => content.Content)
                    .IsRequired();
            }
        );
    }
}
