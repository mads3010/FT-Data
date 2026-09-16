-- Parties across sessions, keyed by group short name. Name and id come from the most recent group actor.
TRUNCATE parties;
INSERT INTO parties (short_name, name, latest_group_actor_id, first_seen, last_seen)
SELECT DISTINCT ON (g.group_short_name)
    g.group_short_name,
    g.name,
    g.id,
    (MIN(g.start_date) OVER (PARTITION BY g.group_short_name))::date,
    (MAX(COALESCE(g.end_date, g.start_date)) OVER (PARTITION BY g.group_short_name))::date
FROM actors g
WHERE g.type_id = 4 AND g.group_short_name IS NOT NULL AND g.group_short_name <> ''
ORDER BY g.group_short_name, g.start_date DESC NULLS LAST, g.id DESC;
