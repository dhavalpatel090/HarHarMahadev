using Khaparwada.Web.Models.Common;
using Khaparwada.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khaparwada.Web.Data.Configurations;

public class GalleryAlbumConfiguration : IEntityTypeConfiguration<GalleryAlbum>
{
    public void Configure(EntityTypeBuilder<GalleryAlbum> builder)
    {
        builder.Property(a => a.Title).HasMaxLength(FieldLengths.Title);
        builder.Property(a => a.TitleGu).HasMaxLength(FieldLengths.Title);
        builder.Property(a => a.Slug).HasMaxLength(FieldLengths.Slug);
        // Description and DescriptionGu stay nvarchar(max).

        builder.HasOne(a => a.Category)
            .WithMany(c => c.Albums)
            .HasForeignKey(a => a.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Event)
            .WithMany(e => e.Albums)
            .HasForeignKey(a => a.EventId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(a => a.Slug).IsUnique().HasFilter(SqlConstraints.NotDeleted);

        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}
