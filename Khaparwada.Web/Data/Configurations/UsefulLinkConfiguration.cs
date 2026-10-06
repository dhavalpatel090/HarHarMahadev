using Khaparwada.Web.Models.Common;
using Khaparwada.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khaparwada.Web.Data.Configurations;

public class UsefulLinkConfiguration : IEntityTypeConfiguration<UsefulLink>
{
    public void Configure(EntityTypeBuilder<UsefulLink> builder)
    {
        builder.Property(l => l.Title).HasMaxLength(FieldLengths.Title);
        builder.Property(l => l.TitleGu).HasMaxLength(FieldLengths.Title);
        builder.Property(l => l.Url).HasMaxLength(FieldLengths.Url);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_UsefulLinks_UrlIsHttp", SqlConstraints.HttpUrl(nameof(UsefulLink.Url))));
    }
}
