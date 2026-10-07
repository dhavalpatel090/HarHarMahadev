using Khaparwada.Web.Models.Common;
using Khaparwada.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khaparwada.Web.Data.Configurations;

public class GalleryImageConfiguration : IEntityTypeConfiguration<GalleryImage>
{
    public void Configure(EntityTypeBuilder<GalleryImage> builder)
    {
        builder.Property(i => i.ImagePath).HasMaxLength(FieldLengths.FilePath);
        builder.Property(i => i.Caption).HasMaxLength(FieldLengths.Caption);
        builder.Property(i => i.CaptionGu).HasMaxLength(FieldLengths.Caption);

        builder.HasOne(i => i.Album)
            .WithMany(a => a.Images)
            .HasForeignKey(i => i.AlbumId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.ReviewedBy)
            .WithMany()
            .HasForeignKey(i => i.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Accounts are switched off, never deleted (see ApplicationUser), so Restrict never blocks anything.
        builder.HasOne(i => i.UploadedBy)
            .WithMany()
            .HasForeignKey(i => i.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.AlbumId, i.Status, i.SortOrder });

        // Hidden when the photo itself or its album is deleted.
        builder.HasQueryFilter(i => !i.IsDeleted && !i.Album.IsDeleted);
    }
}
