using Khaparwada.Web.Models.Common;
using Khaparwada.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khaparwada.Web.Data.Configurations;

public class PlaceImageConfiguration : IEntityTypeConfiguration<PlaceImage>
{
    public void Configure(EntityTypeBuilder<PlaceImage> builder)
    {
        builder.Property(i => i.ImagePath).HasMaxLength(FieldLengths.FilePath);
        builder.Property(i => i.Caption).HasMaxLength(FieldLengths.Caption);
        builder.Property(i => i.CaptionGu).HasMaxLength(FieldLengths.Caption);

        builder.HasOne(i => i.Place)
            .WithMany(p => p.Images)
            .HasForeignKey(i => i.PlaceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Same visibility as the parent: photos of a deleted place disappear with it.
        builder.HasQueryFilter(i => !i.Place.IsDeleted);
    }
}
