# Data model

All tables use snake_case (EFCore.NamingConventions). Ids are the oda.ft.dk ids; there are deliberately no foreign keys between synced tables, because the API can reference rows that have not been synced yet.

## Synced tables

| Table | Source | Key columns |
|---|---|---|
| `periods` | Periode | `code`, `title`, `type`, `start_date`, `end_date` |
| `actors` | Aktør | `type_id`, `group_short_name`, `name`, `biography_xml`, `period_id`, `start_date`, `end_date`, `picture_url`, `biography_party_short_name` |
| `actor_relations` | AktørAktør | `from_actor_id`, `to_actor_id`, `role_id`, `start_date`, `end_date` |
| `meetings` | Møde | `title`, `number`, `date`, `status_id`, `type_id`, `period_id` |
| `cases` | Sag | `type_id`, `status_id`, `title`, `short_title`, `number`, `summary` (resume), `voting_conclusion`, `period_id`, `law_number`, `law_date`, `retsinformation_url` |
| `case_steps` | Sagstrin | `case_id`, `title`, `date`, `type_id`, `status_id` |
| `case_actors` | SagAktør | `case_id`, `actor_id`, `role_id` |
| `votes` | Afstemning | `number`, `conclusion`, `passed`, `type_id`, `meeting_id`, `case_step_id` |
| `ballots` | Stemme | `vote_id`, `actor_id`, `type_id` (1 for, 2 against, 3 absent, 4 abstain) |
| `lookups` | 12 code tables | `(kind, id)` → `name` |
| `sync_states` | — | per entity: `full_load_completed`, `last_id`, `last_updated_at`, run times, `rows_upserted` |

Every synced row keeps `updated_at` = the API's `opdateringsdato`.

## Derived tables (rebuilt by the stats refresh)

| Table | Meaning |
|---|---|
| `parties` | one row per `group_short_name`: display name, latest group actor, first/last seen |
| `party_memberships` | person × party span: `party_short_name`, `period_id`, `start_date`, `end_date`, `source` (1 API relation, 2 biography term, 3 biography party, undated) |
| `biography_memberships` | terms parsed from each person's biography: `party_name`, `party_short_name`, `constituency`, `start_date`, `end_date` (written by the actor sync) |

## Materialized views

| View | Grain | Columns |
|---|---|---|
| `mv_ballots` | ballot | `vote_id`, `actor_id`, `ballot_type`, `vote_date`, `period_id`, `party_short_name` (party on the day) |
| `mv_vote_totals` | vote | for/against/abstain/absent counts |
| `mv_vote_party_breakdown` | vote × party | counts + `majority_ballot_type` (NULL on tie / nobody present) |
| `mv_politician_stats` | person × session | counts, `with_party_count`, `against_party_count` |
| `mv_party_stats` | party × session | `members`, `ballots`, `present_ballots`, `with_majority`, `against_majority` |

## Application tables

| Table | Purpose |
|---|---|
| `bill_summaries` | cache for generated summaries (`case_id`, provider, model, prompt version, `content_json` jsonb, source URL). Empty in v1. |
| `party_accounts` | one party's accounts for one year (`year`, `party_name`, `party_short_name`, `source_file`, `source_page`) |
| `party_donations` | disclosed contributions (`donor_name`, `donor_address`, `amount`, `note`, `source_page`, `raw_text`) |

## Enum mapping

C# enums in `FolketingetVotes.Core.Enums` mirror the API ids: `BallotType`, `VoteType`, `CaseType`, `ActorType`, `ActorRelationRole` (subset), `CaseActorRole` (subset). Names for every other id come from `lookups`.
