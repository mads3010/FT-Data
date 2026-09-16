-- Derived statistics. Rebuilt from scratch on every refresh so definition changes never need a migration.
DROP MATERIALIZED VIEW IF EXISTS mv_party_stats;
DROP MATERIALIZED VIEW IF EXISTS mv_politician_stats;
DROP MATERIALIZED VIEW IF EXISTS mv_vote_party_breakdown;
DROP MATERIALIZED VIEW IF EXISTS mv_vote_totals;
DROP MATERIALIZED VIEW IF EXISTS mv_ballots;

-- Every ballot together with the member's party on the day of the vote (API relation preferred, biography as fallback).
CREATE MATERIALIZED VIEW mv_ballots AS
SELECT
    b.id        AS ballot_id,
    b.vote_id,
    b.actor_id,
    b.type_id   AS ballot_type,
    m.date      AS vote_date,
    m.period_id,
    pm.party_short_name
FROM ballots b
JOIN votes v    ON v.id = b.vote_id
JOIN meetings m ON m.id = v.meeting_id
LEFT JOIN LATERAL (
    SELECT pm.party_short_name
    FROM party_memberships pm
    WHERE pm.person_id = b.actor_id
      AND pm.start_date <= m.date::date
      AND (pm.end_date IS NULL OR pm.end_date >= m.date::date)
    ORDER BY pm.source, pm.start_date DESC, pm.id DESC
    LIMIT 1
) pm ON TRUE;
CREATE UNIQUE INDEX ix_mv_ballots_ballot ON mv_ballots (ballot_id);
CREATE INDEX ix_mv_ballots_vote ON mv_ballots (vote_id);
CREATE INDEX ix_mv_ballots_actor_date ON mv_ballots (actor_id, vote_date DESC);
CREATE INDEX ix_mv_ballots_party_period ON mv_ballots (party_short_name, period_id);

CREATE MATERIALIZED VIEW mv_vote_totals AS
SELECT
    vote_id,
    COUNT(*) FILTER (WHERE ballot_type = 1)::int AS for_count,
    COUNT(*) FILTER (WHERE ballot_type = 2)::int AS against_count,
    COUNT(*) FILTER (WHERE ballot_type = 4)::int AS abstain_count,
    COUNT(*) FILTER (WHERE ballot_type = 3)::int AS absent_count
FROM mv_ballots
GROUP BY vote_id;
CREATE UNIQUE INDEX ix_mv_vote_totals_vote ON mv_vote_totals (vote_id);

-- Per vote and party: counts plus the majority ballot among present members (NULL on a tie or when nobody was present).
CREATE MATERIALIZED VIEW mv_vote_party_breakdown AS
SELECT
    s.vote_id,
    s.party_short_name,
    s.for_count,
    s.against_count,
    s.abstain_count,
    s.absent_count,
    CASE
        WHEN GREATEST(s.for_count, s.against_count, s.abstain_count) = 0 THEN NULL
        WHEN (s.for_count = GREATEST(s.for_count, s.against_count, s.abstain_count))::int
           + (s.against_count = GREATEST(s.for_count, s.against_count, s.abstain_count))::int
           + (s.abstain_count = GREATEST(s.for_count, s.against_count, s.abstain_count))::int > 1 THEN NULL
        WHEN s.for_count = GREATEST(s.for_count, s.against_count, s.abstain_count) THEN 1
        WHEN s.against_count = GREATEST(s.for_count, s.against_count, s.abstain_count) THEN 2
        ELSE 4
    END AS majority_ballot_type
FROM (
    SELECT
        vote_id,
        party_short_name,
        COUNT(*) FILTER (WHERE ballot_type = 1)::int AS for_count,
        COUNT(*) FILTER (WHERE ballot_type = 2)::int AS against_count,
        COUNT(*) FILTER (WHERE ballot_type = 4)::int AS abstain_count,
        COUNT(*) FILTER (WHERE ballot_type = 3)::int AS absent_count
    FROM mv_ballots
    GROUP BY vote_id, party_short_name
) s;
CREATE INDEX ix_mv_vote_party_breakdown_vote ON mv_vote_party_breakdown (vote_id);

-- Per politician and session: counts and agreement with the party majority (present ballots only).
CREATE MATERIALIZED VIEW mv_politician_stats AS
SELECT
    b.actor_id,
    b.period_id,
    COUNT(*)::int AS total,
    COUNT(*) FILTER (WHERE b.ballot_type = 1)::int AS for_count,
    COUNT(*) FILTER (WHERE b.ballot_type = 2)::int AS against_count,
    COUNT(*) FILTER (WHERE b.ballot_type = 4)::int AS abstain_count,
    COUNT(*) FILTER (WHERE b.ballot_type = 3)::int AS absent_count,
    COUNT(*) FILTER (WHERE b.ballot_type <> 3 AND p.majority_ballot_type IS NOT NULL AND b.ballot_type = p.majority_ballot_type)::int AS with_party_count,
    COUNT(*) FILTER (WHERE b.ballot_type <> 3 AND p.majority_ballot_type IS NOT NULL AND b.ballot_type <> p.majority_ballot_type)::int AS against_party_count
FROM mv_ballots b
LEFT JOIN mv_vote_party_breakdown p
    ON p.vote_id = b.vote_id AND b.party_short_name IS NOT NULL AND p.party_short_name = b.party_short_name
GROUP BY b.actor_id, b.period_id;
CREATE UNIQUE INDEX ix_mv_politician_stats_actor_period ON mv_politician_stats (actor_id, period_id);

-- Per party and session: attendance and cohesion.
CREATE MATERIALIZED VIEW mv_party_stats AS
SELECT
    b.party_short_name,
    b.period_id,
    COUNT(DISTINCT b.actor_id)::int AS members,
    COUNT(*)::int AS ballots,
    COUNT(*) FILTER (WHERE b.ballot_type <> 3)::int AS present_ballots,
    COUNT(*) FILTER (WHERE b.ballot_type <> 3 AND p.majority_ballot_type IS NOT NULL AND b.ballot_type = p.majority_ballot_type)::int AS with_majority,
    COUNT(*) FILTER (WHERE b.ballot_type <> 3 AND p.majority_ballot_type IS NOT NULL AND b.ballot_type <> p.majority_ballot_type)::int AS against_majority
FROM mv_ballots b
JOIN mv_vote_party_breakdown p ON p.vote_id = b.vote_id AND p.party_short_name = b.party_short_name
WHERE b.party_short_name IS NOT NULL
GROUP BY b.party_short_name, b.period_id;
CREATE UNIQUE INDEX ix_mv_party_stats_party_period ON mv_party_stats (party_short_name, period_id);
