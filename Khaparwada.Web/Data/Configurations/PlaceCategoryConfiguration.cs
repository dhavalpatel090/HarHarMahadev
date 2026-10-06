using Khaparwada.Web.Models.Common;
using Khaparwada.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khaparwada.Web.Data.Configurations;

public class PlaceCategoryConfiguration : IEntityTypeConfiguration<PlaceCategory>
{
    public void Configure(EntityTypeBuilder<PlaceCategory> builder)
    {
        builder.Property(c => c.Name).HasMaxLength(FieldLengths.Name);
        builder.Property(c => c.NameGu).HasMaxLength(FieldLengths.Name);
        builder.Property(c => c.Slug).HasMaxLength(FieldLengths.Slug);
        builder.Property(c => c.IconClass).HasMaxLength(FieldLengths.IconClass);
        builder.Property(c => c.MarkerColor).HasMaxLength(FieldLengths.HexColor);

        builder.HasIndex(c => c.Slug).IsUnique();

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_PlaceCategories_MarkerColor", SqlConstraints.HexColor(nameof(PlaceCategory.MarkerColor))));
    }
}
