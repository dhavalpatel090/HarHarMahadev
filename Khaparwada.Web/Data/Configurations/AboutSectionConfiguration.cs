using Khaparwada.Web.Models.Common;
using Khaparwada.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khaparwada.Web.Data.Configurations;

public class AboutSectionConfiguration : IEntityTypeConfiguration<AboutSection>
{
    public void Configure(EntityTypeBuilder<AboutSection> builder)
    {
        builder.Property(a => a.Slug).HasMaxLength(FieldLengths.Slug);
        builder.Property(a => a.Title).HasMaxLength(FieldLengths.Title);
        builder.Property(a => a.TitleGu).HasMaxLength(FieldLengths.Title);
        builder.Property(a => a.ImagePath).HasMaxLength(FieldLengths.FilePath);
        // Body and BodyGu stay nvarchar(max).

        builder.HasIndex(a => a.Slug).IsUnique().HasFilter(SqlConstraints.NotDeleted);

        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}
