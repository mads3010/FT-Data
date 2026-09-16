-- One row per (person, parliamentary group of one session) from the API's group relations (source 1),
-- plus terms stated in members' biographies (source 2) as a fallback: the API has no group relation at all
-- for roughly one in eight voting members. Relation dates win; the group's own session dates fill gaps.
TRUNCATE party_memberships;
INSERT INTO party_memberships (person_id, group_actor_id, party_short_name, period_id, start_date, end_date, source)
SELECT
    r.to_actor_id,
    g.id,
    g.group_short_name,
    g.period_id,
    COALESCE(r.start_date, g.start_date)::date,
    COALESCE(r.end_date, g.end_date)::date,
    1
FROM actor_relations r
JOIN actors g ON g.id = r.from_actor_id AND g.type_id = 4
JOIN actors p ON p.id = r.to_actor_id AND p.type_id = 5
WHERE r.role_id = 15
  AND g.group_short_name IS NOT NULL AND g.group_short_name <> ''
  AND COALESCE(r.start_date, g.start_date) IS NOT NULL;

INSERT INTO party_memberships (person_id, group_actor_id, party_short_name, period_id, start_date, end_date, source)
SELECT
    bm.person_id,
    0,
    bm.party_short_name,
    NULL,
    bm.start_date,
    bm.end_date,
    2
FROM biography_memberships bm
WHERE bm.party_short_name IS NOT NULL;

-- Source 3 (last resort): the party the biography names as the member's party, valid for the member's whole
-- record. Only reached for people with neither a group relation nor a constituency entry (a handful of former members).
INSERT INTO party_memberships (person_id, group_actor_id, party_short_name, period_id, start_date, end_date, source)
SELECT a.id, 0, a.biography_party_short_name, NULL, DATE '1900-01-01', NULL, 3
FROM actors a
WHERE a.type_id = 5 AND a.biography_party_short_name IS NOT NULL AND a.biography_party_short_name <> ''
  AND a.biography_party_short_name IN (SELECT short_name FROM parties);
