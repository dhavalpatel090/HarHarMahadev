using Khaparwada.Web.Models.Common;
using Khaparwada.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khaparwada.Web.Data.Configurations;

public class MemoryImageConfiguration : IEntityTypeConfiguration<MemoryImage>
{
    public void Configure(EntityTypeBuilder<MemoryImage> builder)
    {
        builder.Property(i => i.ImagePath).HasMaxLength(FieldLengths.FilePath);
        builder.Property(i => i.Caption).HasMaxLength(FieldLengths.Caption);

        builder.HasOne(i => i.MemorySubmission)
            .WithMany(m => m.Images)
            .HasForeignKey(i => i.MemorySubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Same visibility as the parent: photos of a deleted memory disappear with it.
        builder.HasQueryFilter(i => !i.MemorySubmission.IsDeleted);
    }
}
