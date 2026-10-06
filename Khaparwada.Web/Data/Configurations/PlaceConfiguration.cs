using Khaparwada.Web.Models.Common;
using Khaparwada.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khaparwada.Web.Data.Configurations;

public class PlaceConfiguration : IEntityTypeConfiguration<Place>
{
    public void Configure(EntityTypeBuilder<Place> builder)
    {
        builder.Property(p => p.Name).HasMaxLength(FieldLengths.Name);
        builder.Property(p => p.NameGu).HasMaxLength(FieldLengths.Name);
        builder.Property(p => p.Slug).HasMaxLength(FieldLengths.Slug);
        builder.Property(p => p.Summary).HasMaxLength(FieldLengths.Summary);
        builder.Property(p => p.SummaryGu).HasMaxLength(FieldLengths.Summary);
        builder.Property(p => p.Address).HasMaxLength(FieldLengths.Address);
        builder.Property(p => p.AddressGu).HasMaxLength(FieldLengths.Address);
        builder.Property(p => p.Latitude).HasPrecision(9, 6);   // about 0.1 m accuracy
        builder.Property(p => p.Longitude).HasPrecision(9, 6);
        builder.Property(p => p.MainImagePath).HasMaxLength(FieldLengths.FilePath);
        builder.Property(p => p.Phone).HasMaxLength(FieldLengths.Phone);
        builder.Property(p => p.Email).HasMaxLength(FieldLengths.Email);
        builder.Property(p => p.Website).HasMaxLength(FieldLengths.Url);
        builder.Property(p => p.Timings).HasMaxLength(FieldLengths.Timings);
        builder.Property(p => p.TimingsGu).HasMaxLength(FieldLengths.Timings);
        // Description and DescriptionGu stay nvarchar(max).

        // Restrict: deleting a category must never delete its places.
        builder.HasOne(p => p.Category)
            .WithMany(c => c.Places)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.Slug).IsUnique().HasFilter(SqlConstraints.NotDeleted);
        builder.HasIndex(p => new { p.CategoryId, p.IsPublished });

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Places_Coordinates", SqlConstraints.Coordinates);
            t.HasCheckConstraint("CK_Places_WebsiteIsHttp", SqlConstraints.HttpUrl(nameof(Place.Website)));
        });

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
