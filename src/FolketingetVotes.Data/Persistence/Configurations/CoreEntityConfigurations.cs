using FolketingetVotes.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FolketingetVotes.Data.Persistence.Configurations;

internal sealed class PeriodConfiguration : IEntityTypeConfiguration<Period>
{
    public void Configure(EntityTypeBuilder<Period> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => x.Code);
    }
}

internal sealed class ActorConfiguration : IEntityTypeConfiguration<Actor>
{
    public void Configure(EntityTypeBuilder<Actor> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.TypeId).HasConversion<int>();
        b.HasIndex(x => x.TypeId);
        b.HasIndex(x => new { x.TypeId, x.GroupShortName });
        b.HasIndex(x => x.Name);
        b.HasIndex(x => x.Name).HasDatabaseName("ix_actors_name_trgm").HasMethod("gin").HasOperators("gin_trgm_ops");
    }
}

internal sealed class ActorRelationConfiguration : IEntityTypeConfiguration<ActorRelation>
{
    public void Configure(EntityTypeBuilder<ActorRelation> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => new { x.FromActorId, x.RoleId });
        b.HasIndex(x => new { x.ToActorId, x.RoleId });
    }
}

internal sealed class MeetingConfiguration : IEntityTypeConfiguration<Meeting>
{
    public void Configure(EntityTypeBuilder<Meeting> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => x.Date);
        b.HasIndex(x => x.PeriodId);
    }
}

internal sealed class ParliamentaryCaseConfiguration : IEntityTypeConfiguration<ParliamentaryCase>
{
    public void Configure(EntityTypeBuilder<ParliamentaryCase> b)
    {
        b.ToTable("cases");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.TypeId).HasConversion<int>();
        b.HasIndex(x => x.PeriodId);
        b.HasIndex(x => new { x.TypeId, x.PeriodId });
        b.HasIndex(x => x.Title).HasDatabaseName("ix_cases_title_trgm").HasMethod("gin").HasOperators("gin_trgm_ops");
    }
}

internal sealed class CaseStepConfiguration : IEntityTypeConfiguration<CaseStep>
{
    public void Configure(EntityTypeBuilder<CaseStep> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => x.CaseId);
    }
}

internal sealed class CaseActorConfiguration : IEntityTypeConfiguration<CaseActor>
{
    public void Configure(EntityTypeBuilder<CaseActor> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => x.CaseId);
        b.HasIndex(x => new { x.ActorId, x.RoleId });
    }
}

internal sealed class VoteConfiguration : IEntityTypeConfiguration<Vote>
{
    public void Configure(EntityTypeBuilder<Vote> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.TypeId).HasConversion<int>();
        b.HasIndex(x => x.MeetingId);
        b.HasIndex(x => x.CaseStepId);
    }
}

internal sealed class BallotConfiguration : IEntityTypeConfiguration<Ballot>
{
    public void Configure(EntityTypeBuilder<Ballot> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.TypeId).HasConversion<int>();
        b.HasIndex(x => x.VoteId);
        b.HasIndex(x => new { x.ActorId, x.VoteId });
    }
}

internal sealed class LookupConfiguration : IEntityTypeConfiguration<Lookup>
{
    public void Configure(EntityTypeBuilder<Lookup> b)
    {
        b.HasKey(x => new { x.Kind, x.Id });
        b.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32);
    }
}

internal sealed class SyncStateConfiguration : IEntityTypeConfiguration<SyncState>
{
    public void Configure(EntityTypeBuilder<SyncState> b)
    {
        b.HasKey(x => x.EntityName);
        b.Property(x => x.EntityName).HasMaxLength(64);
        b.Property(x => x.LastRunStartedAt).HasColumnType("timestamp with time zone");
        b.Property(x => x.LastRunCompletedAt).HasColumnType("timestamp with time zone");
    }
}

internal sealed class PartyConfiguration : IEntityTypeConfiguration<Party>
{
    public void Configure(EntityTypeBuilder<Party> b)
    {
        b.HasKey(x => x.ShortName);
    }
}

internal sealed class PartyMembershipConfiguration : IEntityTypeConfiguration<PartyMembership>
{
    public void Configure(EntityTypeBuilder<PartyMembership> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.PersonId, x.StartDate });
        b.HasIndex(x => new { x.PartyShortName, x.PeriodId });
    }
}

internal sealed class BiographyMembershipConfiguration : IEntityTypeConfiguration<BiographyMembership>
{
    public void Configure(EntityTypeBuilder<BiographyMembership> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.PersonId, x.StartDate });
    }
}

internal sealed class KeywordConfiguration : IEntityTypeConfiguration<Keyword>
{
    public void Configure(EntityTypeBuilder<Keyword> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => x.Name);
        b.HasIndex(x => x.Name).HasDatabaseName("ix_keywords_name_trgm").HasMethod("gin").HasOperators("gin_trgm_ops");
    }
}

internal sealed class CaseKeywordConfiguration : IEntityTypeConfiguration<CaseKeyword>
{
    public void Configure(EntityTypeBuilder<CaseKeyword> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => x.CaseId);
        b.HasIndex(x => x.KeywordId);
    }
}

internal sealed class RolePeriodConfiguration : IEntityTypeConfiguration<RolePeriod>
{
    public void Configure(EntityTypeBuilder<RolePeriod> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Kind).HasConversion<int>();
        b.HasIndex(x => new { x.PersonId, x.Kind, x.StartDate });
    }
}

internal sealed class BillSummaryConfiguration : IEntityTypeConfiguration<BillSummary>
{
    public void Configure(EntityTypeBuilder<BillSummary> b)
    {
        b.HasKey(x => x.CaseId);
        b.Property(x => x.CaseId).ValueGeneratedNever();
        b.Property(x => x.Provider).HasMaxLength(64);
        b.Property(x => x.Model).HasMaxLength(64);
        b.Property(x => x.PromptVersion).HasMaxLength(32);
        b.Property(x => x.SourceUrl).HasMaxLength(512);
        b.Property(x => x.ContentJson).HasColumnType("jsonb");
        b.Property(x => x.GeneratedAt).HasColumnType("timestamp with time zone");
    }
}

internal sealed class PartyAccountConfiguration : IEntityTypeConfiguration<PartyAccount>
{
    public void Configure(EntityTypeBuilder<PartyAccount> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.PartyName).HasMaxLength(256);
        b.Property(x => x.SourceFile).HasMaxLength(512);
        b.Property(x => x.ImportedAt).HasColumnType("timestamp with time zone");
        b.HasIndex(x => new { x.Year, x.PartyName }).IsUnique();
        b.HasMany(x => x.Donations).WithOne(x => x.PartyAccount).HasForeignKey(x => x.PartyAccountId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PartyDonationConfiguration : IEntityTypeConfiguration<PartyDonation>
{
    public void Configure(EntityTypeBuilder<PartyDonation> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.DonorName).HasMaxLength(512);
        b.Property(x => x.DonorAddress).HasMaxLength(512);
        b.Property(x => x.Currency).HasMaxLength(8);
        b.Property(x => x.Amount).HasPrecision(14, 2);
        b.HasIndex(x => x.DonorName);
    }
}
