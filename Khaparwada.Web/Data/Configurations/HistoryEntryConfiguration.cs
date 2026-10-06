using Khaparwada.Web.Models.Common;
using Khaparwada.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khaparwada.Web.Data.Configurations;

public class HistoryEntryConfiguration : IEntityTypeConfiguration<HistoryEntry>
{
    public void Configure(EntityTypeBuilder<HistoryEntry> builder)
    {
        builder.Property(h => h.YearLabel).HasMaxLength(FieldLengths.Label);
        builder.Property(h => h.YearLabelGu).HasMaxLength(FieldLengths.Label);
        builder.Property(h => h.Title).HasMaxLength(FieldLengths.Title);
        builder.Property(h => h.TitleGu).HasMaxLength(FieldLengths.Title);
        builder.Property(h => h.ImagePath).HasMaxLength(FieldLengths.FilePath);

        builder.HasIndex(h => new { h.IsPublished, h.SortYear });

        builder.HasQueryFilter(h => !h.IsDeleted);
    }
}
