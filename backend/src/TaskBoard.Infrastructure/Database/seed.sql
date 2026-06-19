with demo_user as (
    insert into users (id, email, password_hash, created_at)
    values (
        '11111111-1111-1111-1111-111111111111',
        'demo@example.com',
        'AQAAAAIAAYagAAAAEFX60bLH6gNjjrtYGH2VA+E1rosVRAEivxE7vtTUJIssCv0njAZqwERLk2hPt1tPAA==',
        '2026-01-01T00:00:00Z'
    )
    on conflict (email) do update
    set password_hash = excluded.password_hash
    returning id
)
insert into tasks (id, user_id, title, description, status, due_date, created_at, updated_at)
select
    task_seed.id,
    demo_user.id,
    task_seed.title,
    task_seed.description,
    task_seed.status,
    task_seed.due_date,
    task_seed.created_at,
    task_seed.updated_at
from demo_user
cross join (
    values
    (
        '22222222-2222-2222-2222-222222222221'::uuid,
        'Review TaskBoard requirements',
        'Read the technical exercise scope and confirm the first implementation slice.',
        'Todo',
        '2026-01-05T09:00:00Z'::timestamptz,
        '2026-01-01T00:05:00Z'::timestamptz,
        null::timestamptz
    ),
    (
        '22222222-2222-2222-2222-222222222222'::uuid,
        'Implement repository layer',
        'Use raw Npgsql with parameterized SQL in Infrastructure.',
        'InProgress',
        '2026-01-06T12:00:00Z'::timestamptz,
        '2026-01-01T00:10:00Z'::timestamptz,
        null::timestamptz
    ),
    (
        '22222222-2222-2222-2222-222222222223'::uuid,
        'Document validation commands',
        'Record how dotnet test was used to validate the backend slice.',
        'Done',
        '2026-01-07T17:00:00Z'::timestamptz,
        '2026-01-01T00:15:00Z'::timestamptz,
        '2026-01-01T00:20:00Z'::timestamptz
    )
) as task_seed(id, title, description, status, due_date, created_at, updated_at)
on conflict (id) do nothing;
