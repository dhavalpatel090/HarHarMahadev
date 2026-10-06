using Khaparwada.Web.Models.Common;
using Khaparwada.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khaparwada.Web.Data.Configurations;

public class MemorySubmissionConfiguration : IEntityTypeConfiguration<MemorySubmission>
{
    public void Configure(EntityTypeBuilder<MemorySubmission> builder)
    {
        builder.Property(m => m.Title).HasMaxLength(FieldLengths.Title);
        builder.Property(m => m.EraLabel).HasMaxLength(FieldLengths.Label);
        builder.Property(m => m.SubmitterName).HasMaxLength(FieldLengths.Name);
        builder.Property(m => m.SubmitterContact).HasMaxLength(FieldLengths.Email);
        builder.Property(m => m.ReviewNote).HasMaxLength(FieldLengths.ReviewNote);
        builder.Property(m => m.Slug).HasMaxLength(FieldLengths.Slug);
        // Story stays nvarchar(max).

        builder.HasOne(m => m.ReviewedBy)
            .WithMany()
            .HasForeignKey(m => m.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // The slug only exists once the memory is published.
        builder.HasIndex(m => m.Slug).IsUnique().HasFilter($"[Slug] IS NOT NULL AND {SqlConstraints.NotDeleted}");
        builder.HasIndex(m => new { m.Status, m.CreatedAt });

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_MemorySubmissions_ConsentBeforeApproval",
            SqlConstraints.ConsentRequired(nameof(MemorySubmission.ConsentToPublish))));

        builder.HasQueryFilter(m => !m.IsDeleted);
    }
}
