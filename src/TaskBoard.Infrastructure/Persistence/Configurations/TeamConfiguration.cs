using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoard.Domain.Teams;
using TaskBoard.Domain.Users;

namespace TaskBoard.Infrastructure.Persistence.Configurations;

internal sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).HasMaxLength(100).IsRequired();

        builder.HasMany(t => t.Members)
            .WithOne()
            .HasForeignKey(m => m.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(20);

        builder.HasOne<User>().WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(m => new { m.TeamId, m.UserId }).IsUnique();
        builder.HasIndex(m => m.UserId);
    }
}
