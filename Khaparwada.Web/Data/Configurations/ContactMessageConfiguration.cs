using Khaparwada.Web.Models.Common;
using Khaparwada.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khaparwada.Web.Data.Configurations;

public class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.Property(m => m.Name).HasMaxLength(FieldLengths.Name);
        builder.Property(m => m.Email).HasMaxLength(FieldLengths.Email);
        builder.Property(m => m.Phone).HasMaxLength(FieldLengths.Phone);
        builder.Property(m => m.Subject).HasMaxLength(FieldLengths.Title);
        builder.Property(m => m.Message).HasMaxLength(FieldLengths.ContactMessage);

        builder.HasIndex(m => new { m.IsRead, m.CreatedAt });

        builder.HasQueryFilter(m => !m.IsDeleted);
    }
}
