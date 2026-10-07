using Khaparwada.Web.Models.Common;
using Khaparwada.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Khaparwada.Web.Data.Configurations;

public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.Property(p => p.FullName).HasMaxLength(FieldLengths.Name);
        builder.Property(p => p.FullNameGu).HasMaxLength(FieldLengths.Name);
        builder.Property(p => p.Slug).HasMaxLength(FieldLengths.Slug);
        builder.Property(p => p.Profession).HasMaxLength(FieldLengths.Label);
        builder.Property(p => p.ProfessionGu).HasMaxLength(FieldLengths.Label);
        builder.Property(p => p.ShortBio).HasMaxLength(FieldLengths.Intro);
        builder.Property(p => p.ShortBioGu).HasMaxLength(FieldLengths.Intro);
        builder.Property(p => p.Achievement).HasMaxLength(FieldLengths.Intro);
        builder.Property(p => p.AchievementGu).HasMaxLength(FieldLengths.Intro);
        builder.Property(p => p.Contribution).HasMaxLength(FieldLengths.Intro);
        builder.Property(p => p.ContributionGu).HasMaxLength(FieldLengths.Intro);
        builder.Property(p => p.PhotoPath).HasMaxLength(FieldLengths.FilePath);
        builder.Property(p => p.ConsentNote).HasMaxLength(FieldLengths.ConsentNote);
        builder.Property(p => p.ReviewNote).HasMaxLength(FieldLengths.ReviewNote);

        builder.HasOne(p => p.Category)
            .WithMany(c => c.People)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Users are deactivated, never deleted, so the reviewer reference always stays valid.
        builder.HasOne(p => p.ReviewedBy)
            .WithMany()
            .HasForeignKey(p => p.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.Slug).IsUnique().HasFilter(SqlConstraints.NotDeleted);
        builder.HasIndex(p => new { p.CategoryId, p.Status });

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_People_ConsentBeforeApproval", SqlConstraints.ConsentRequired(nameof(Person.HasConsent))));

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
