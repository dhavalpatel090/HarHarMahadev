using Khaparwada.Web.Models.Common;
using Khaparwada.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khaparwada.Web.Data.Configurations;

public class VillageInfoConfiguration : IEntityTypeConfiguration<VillageInfo>
{
    public void Configure(EntityTypeBuilder<VillageInfo> builder)
    {
        // Single-row table: the id is always 1 (not an identity column, whose numbers can skip after a
        // failed insert), and a check constraint stops a second row from ever being added.
        builder.Property(v => v.Id).ValueGeneratedNever();

        builder.Property(v => v.Name).HasMaxLength(FieldLengths.Name);
        builder.Property(v => v.NameGu).HasMaxLength(FieldLengths.Name);
        builder.Property(v => v.ShortIntro).HasMaxLength(FieldLengths.Intro);
        builder.Property(v => v.ShortIntroGu).HasMaxLength(FieldLengths.Intro);
        builder.Property(v => v.AreaSqKm).HasPrecision(9, 2);
        builder.Property(v => v.StatsSource).HasMaxLength(FieldLengths.Title);
        builder.Property(v => v.StatsSourceGu).HasMaxLength(FieldLengths.Title);
        builder.Property(v => v.Taluka).HasMaxLength(FieldLengths.Label);
        builder.Property(v => v.TalukaGu).HasMaxLength(FieldLengths.Label);
        builder.Property(v => v.District).HasMaxLength(FieldLengths.Label);
        builder.Property(v => v.DistrictGu).HasMaxLength(FieldLengths.Label);
        builder.Property(v => v.State).HasMaxLength(FieldLengths.Label);
        builder.Property(v => v.StateGu).HasMaxLength(FieldLengths.Label);
        builder.Property(v => v.PinCode).HasMaxLength(FieldLengths.PinCode);
        builder.Property(v => v.Latitude).HasPrecision(9, 6);
        builder.Property(v => v.Longitude).HasPrecision(9, 6);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_VillageInfo_SingleRow", SqlConstraints.SingleRow);
            t.HasCheckConstraint("CK_VillageInfo_Coordinates", SqlConstraints.Coordinates);
            t.HasCheckConstraint("CK_VillageInfo_PinCode", SqlConstraints.PinCode);
        });
    }
}
