-- Periods in which a person's voting record should be read differently: ministerial posts (from the API's
-- relations to "ministertitel" actors, recorded in either direction) and temporary/substitute membership
-- ("Midlertidigt folketingsmedlem" in the biography).
TRUNCATE role_periods;
INSERT INTO role_periods (person_id, kind, title, start_date, end_date)
SELECT DISTINCT p.id, 1, t.name, r.start_date::date, r.end_date::date
FROM actor_relations r
JOIN actors p ON p.type_id = 5 AND p.id IN (r.from_actor_id, r.to_actor_id)
JOIN actors t ON t.type_id = 2 AND t.id IN (r.from_actor_id, r.to_actor_id)
WHERE r.role_id = 8 AND r.start_date IS NOT NULL;

INSERT INTO role_periods (person_id, kind, title, start_date, end_date)
SELECT bm.person_id, 2, 'Midlertidigt medlem (' || bm.party_name || ')', bm.start_date, bm.end_date
FROM biography_memberships bm
WHERE bm.is_temporary;
