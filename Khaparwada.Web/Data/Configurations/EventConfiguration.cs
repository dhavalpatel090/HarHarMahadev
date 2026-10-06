using Khaparwada.Web.Models.Common;
using Khaparwada.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khaparwada.Web.Data.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.Property(e => e.Title).HasMaxLength(FieldLengths.Title);
        builder.Property(e => e.TitleGu).HasMaxLength(FieldLengths.Title);
        builder.Property(e => e.Slug).HasMaxLength(FieldLengths.Slug);
        builder.Property(e => e.Summary).HasMaxLength(FieldLengths.Summary);
        builder.Property(e => e.SummaryGu).HasMaxLength(FieldLengths.Summary);
        builder.Property(e => e.LocationText).HasMaxLength(FieldLengths.Address);
        builder.Property(e => e.LocationTextGu).HasMaxLength(FieldLengths.Address);
        builder.Property(e => e.ImagePath).HasMaxLength(FieldLengths.FilePath);
        // Description and DescriptionGu stay nvarchar(max).

        // Set null: if the venue is permanently deleted, the event keeps its text location.
        builder.HasOne(e => e.Place)
            .WithMany(p => p.Events)
            .HasForeignKey(e => e.PlaceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.Slug).IsUnique().HasFilter(SqlConstraints.NotDeleted);
        builder.HasIndex(e => new { e.IsPublished, e.StartsAt });

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Events_EndsAfterStart", "[EndsAt] IS NULL OR [EndsAt] >= [StartsAt]"));

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
