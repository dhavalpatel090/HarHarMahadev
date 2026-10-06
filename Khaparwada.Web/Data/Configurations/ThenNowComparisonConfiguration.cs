using Khaparwada.Web.Models.Common;
using Khaparwada.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khaparwada.Web.Data.Configurations;

public class ThenNowComparisonConfiguration : IEntityTypeConfiguration<ThenNowComparison>
{
    public void Configure(EntityTypeBuilder<ThenNowComparison> builder)
    {
        builder.Property(t => t.Title).HasMaxLength(FieldLengths.Title);
        builder.Property(t => t.TitleGu).HasMaxLength(FieldLengths.Title);
        builder.Property(t => t.Caption).HasMaxLength(FieldLengths.Caption);
        builder.Property(t => t.CaptionGu).HasMaxLength(FieldLengths.Caption);
        builder.Property(t => t.ThenImagePath).HasMaxLength(FieldLengths.FilePath);
        builder.Property(t => t.NowImagePath).HasMaxLength(FieldLengths.FilePath);
        builder.Property(t => t.ThenLabel).HasMaxLength(FieldLengths.Label);
        builder.Property(t => t.ThenLabelGu).HasMaxLength(FieldLengths.Label);
        builder.Property(t => t.NowLabel).HasMaxLength(FieldLengths.Label);
        builder.Property(t => t.NowLabelGu).HasMaxLength(FieldLengths.Label);

        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}
