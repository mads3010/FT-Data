-- Derived statistics. Rebuilt from scratch on every refresh so definition changes never need a migration.
DROP MATERIALIZED VIEW IF EXISTS mv_questions;
DROP MATERIALIZED VIEW IF EXISTS mv_topic_stats;
DROP MATERIALIZED VIEW IF EXISTS mv_current_members;
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
    pm.party_short_name,
    EXISTS (
        SELECT 1 FROM role_periods rp
        WHERE rp.person_id = b.actor_id AND rp.kind = 1
          AND rp.start_date <= m.date::date AND (rp.end_date IS NULL OR rp.end_date >= m.date::date)
    ) AS while_minister,
    EXISTS (
        SELECT 1 FROM role_periods rp
        WHERE rp.person_id = b.actor_id AND rp.kind = 3
          AND rp.start_date <= m.date::date AND (rp.end_date IS NULL OR rp.end_date >= m.date::date)
    ) AS while_on_leave
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
    COUNT(*) FILTER (WHERE b.ballot_type <> 3 AND p.majority_ballot_type IS NOT NULL AND b.ballot_type <> p.majority_ballot_type)::int AS against_party_count,
    COUNT(*) FILTER (WHERE b.while_minister)::int AS minister_total,
    COUNT(*) FILTER (WHERE b.while_minister AND b.ballot_type = 3)::int AS minister_absent,
    COUNT(*) FILTER (WHERE b.while_minister OR b.while_on_leave)::int AS role_total,
    COUNT(*) FILTER (WHERE (b.while_minister OR b.while_on_leave) AND b.ballot_type = 3)::int AS role_absent
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

-- Who is a member today: everyone registered (present or absent) in the votes of the most recent sitting day,
-- with the group they were attributed to that day. Every seated member gets a ballot row in every vote, so this
-- is the exact roster (179 incl. serving substitutes) and does not depend on the API's incomplete group relations.
CREATE MATERIALIZED VIEW mv_current_members AS
WITH last_day AS (SELECT MAX(vote_date)::date AS d FROM mv_ballots)
SELECT DISTINCT ON (b.actor_id)
    b.actor_id AS person_id,
    b.party_short_name,
    COALESCE((SELECT MAX(pm.start_date) FROM party_memberships pm
              WHERE pm.person_id = b.actor_id AND pm.party_short_name = b.party_short_name AND pm.source < 3
                AND pm.start_date <= b.vote_date::date), b.vote_date::date) AS start_date
FROM mv_ballots b, last_day
WHERE b.vote_date::date = last_day.d AND b.party_short_name IS NOT NULL
ORDER BY b.actor_id, b.vote_date DESC;
CREATE UNIQUE INDEX ix_mv_current_members_person ON mv_current_members (person_id);

-- Cases and chamber votes per subject keyword.
CREATE MATERIALIZED VIEW mv_topic_stats AS
SELECT ck.keyword_id, COUNT(DISTINCT ck.case_id)::int AS case_count, COUNT(DISTINCT v.id)::int AS vote_count
FROM case_keywords ck
LEFT JOIN case_steps s ON s.case_id = ck.case_id
LEFT JOIN votes v ON v.case_step_id = s.id
GROUP BY ck.keyword_id;
CREATE UNIQUE INDEX ix_mv_topic_stats_keyword ON mv_topic_stats (keyword_id);

-- § 20 questions: who asked whom, when it was submitted (step type 1) and answered (8 written, 19 oral).
CREATE MATERIALIZED VIEW mv_questions AS
SELECT
    c.id AS case_id,
    c.period_id,
    c.number,
    c.title,
    (SELECT ca.actor_id FROM case_actors ca WHERE ca.case_id = c.id AND ca.role_id = 10 ORDER BY ca.id LIMIT 1) AS asker_id,
    (SELECT ca.actor_id FROM case_actors ca JOIN actors a ON a.id = ca.actor_id AND a.type_id = 5 WHERE ca.case_id = c.id AND ca.role_id = 17 ORDER BY ca.id LIMIT 1) AS minister_person_id,
    (SELECT a.name FROM case_actors ca JOIN actors a ON a.id = ca.actor_id WHERE ca.case_id = c.id AND ca.role_id = 14 ORDER BY ca.id LIMIT 1) AS minister_title,
    (SELECT pm.party_short_name FROM party_memberships pm
      WHERE pm.person_id = (SELECT ca.actor_id FROM case_actors ca WHERE ca.case_id = c.id AND ca.role_id = 10 ORDER BY ca.id LIMIT 1)
        AND pm.start_date <= COALESCE((SELECT MIN(s.date) FROM case_steps s WHERE s.case_id = c.id AND s.type_id = 1), CURRENT_DATE)::date
        AND (pm.end_date IS NULL OR pm.end_date >= COALESCE((SELECT MIN(s.date) FROM case_steps s WHERE s.case_id = c.id AND s.type_id = 1), CURRENT_DATE)::date)
      ORDER BY pm.source, pm.start_date DESC LIMIT 1) AS asker_party,
    (SELECT MIN(s.date) FROM case_steps s WHERE s.case_id = c.id AND s.type_id = 1) AS asked_date,
    (SELECT MIN(s.date) FROM case_steps s WHERE s.case_id = c.id AND s.type_id IN (8, 19)) AS answered_date,
    EXISTS (SELECT 1 FROM case_steps s WHERE s.case_id = c.id AND s.type_id = 19) AS oral,
    EXISTS (SELECT 1 FROM case_steps s WHERE s.case_id = c.id AND s.type_id = 27) AS withdrawn
FROM cases c
WHERE c.type_id = 10;
CREATE UNIQUE INDEX ix_mv_questions_case ON mv_questions (case_id);
CREATE INDEX ix_mv_questions_asker ON mv_questions (asker_id);
CREATE INDEX ix_mv_questions_minister ON mv_questions (minister_person_id);
CREATE INDEX ix_mv_questions_period ON mv_questions (period_id);
