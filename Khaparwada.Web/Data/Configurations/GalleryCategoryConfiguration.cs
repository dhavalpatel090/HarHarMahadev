using Khaparwada.Web.Models.Common;
using Khaparwada.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khaparwada.Web.Data.Configurations;

public class GalleryCategoryConfiguration : IEntityTypeConfiguration<GalleryCategory>
{
    public void Configure(EntityTypeBuilder<GalleryCategory> builder)
    {
        builder.Property(c => c.Name).HasMaxLength(FieldLengths.Name);
        builder.Property(c => c.NameGu).HasMaxLength(FieldLengths.Name);
        builder.Property(c => c.Slug).HasMaxLength(FieldLengths.Slug);

        builder.HasIndex(c => c.Slug).IsUnique();
    }
}
