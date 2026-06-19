create table if not exists users
(
    id uuid primary key,
    email text unique not null,
    password_hash text not null,
    created_at timestamptz not null
);

create table if not exists tasks
(
    id uuid primary key,
    user_id uuid not null references users(id),
    title varchar(100) not null,
    description varchar(1000) null,
    status varchar(20) not null,
    due_date timestamptz not null,
    created_at timestamptz not null,
    updated_at timestamptz null,
    constraint ck_tasks_status check (status in ('Todo', 'InProgress', 'Done'))
);

create index if not exists ix_tasks_user_id on tasks(user_id);
