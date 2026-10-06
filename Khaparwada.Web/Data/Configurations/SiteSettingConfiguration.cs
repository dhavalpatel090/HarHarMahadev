using Khaparwada.Web.Models.Common;
using Khaparwada.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khaparwada.Web.Data.Configurations;

public class SiteSettingConfiguration : IEntityTypeConfiguration<SiteSetting>
{
    public void Configure(EntityTypeBuilder<SiteSetting> builder)
    {
        builder.Property(s => s.Id).ValueGeneratedNever(); // single-row table, see VillageInfoConfiguration

        builder.Property(s => s.SiteName).HasMaxLength(FieldLengths.Name);
        builder.Property(s => s.SiteNameGu).HasMaxLength(FieldLengths.Name);
        builder.Property(s => s.Tagline).HasMaxLength(FieldLengths.Title);
        builder.Property(s => s.TaglineGu).HasMaxLength(FieldLengths.Title);
        builder.Property(s => s.OfficeEmail).HasMaxLength(FieldLengths.Email);
        builder.Property(s => s.OfficePhone).HasMaxLength(FieldLengths.Phone);
        builder.Property(s => s.OfficeAddress).HasMaxLength(FieldLengths.Address);
        builder.Property(s => s.OfficeAddressGu).HasMaxLength(FieldLengths.Address);
        builder.Property(s => s.FacebookUrl).HasMaxLength(FieldLengths.Url);
        builder.Property(s => s.InstagramUrl).HasMaxLength(FieldLengths.Url);
        builder.Property(s => s.YouTubeUrl).HasMaxLength(FieldLengths.Url);
        builder.Property(s => s.WhatsAppUrl).HasMaxLength(FieldLengths.Url);
        builder.Property(s => s.HeroImagePath).HasMaxLength(FieldLengths.FilePath);
        builder.Property(s => s.HeroVideoPath).HasMaxLength(FieldLengths.FilePath);
        builder.Property(s => s.DefaultMetaDescription).HasMaxLength(FieldLengths.Summary);
        builder.Property(s => s.DefaultMetaDescriptionGu).HasMaxLength(FieldLengths.Summary);

        string[] linkColumns =
        [
            nameof(SiteSetting.FacebookUrl),
            nameof(SiteSetting.InstagramUrl),
            nameof(SiteSetting.YouTubeUrl),
            nameof(SiteSetting.WhatsAppUrl)
        ];

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_SiteSettings_SingleRow", SqlConstraints.SingleRow);
            t.HasCheckConstraint(
                "CK_SiteSettings_HttpLinks",
                string.Join(" AND ", linkColumns.Select(column => $"({SqlConstraints.HttpUrl(column)})")));
        });
    }
}
