using Khaparwada.Web.Models.Common;
using Khaparwada.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khaparwada.Web.Data.Configurations;

public class NoticeConfiguration : IEntityTypeConfiguration<Notice>
{
    public void Configure(EntityTypeBuilder<Notice> builder)
    {
        builder.Property(n => n.Title).HasMaxLength(FieldLengths.Title);
        builder.Property(n => n.TitleGu).HasMaxLength(FieldLengths.Title);
        builder.Property(n => n.Slug).HasMaxLength(FieldLengths.Slug);
        builder.Property(n => n.ImagePath).HasMaxLength(FieldLengths.FilePath);
        builder.Property(n => n.AttachmentPath).HasMaxLength(FieldLengths.FilePath);
        builder.Property(n => n.AttachmentName).HasMaxLength(FieldLengths.FileName);
        // Description and DescriptionGu stay nvarchar(max).

        builder.HasIndex(n => n.Slug).IsUnique().HasFilter(SqlConstraints.NotDeleted);
        builder.HasIndex(n => new { n.IsPublished, n.PublishedAt });

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Notices_ExpiresAfterPublished", "[ExpiresAt] IS NULL OR [ExpiresAt] > [PublishedAt]"));

        builder.HasQueryFilter(n => !n.IsDeleted);
    }
}
