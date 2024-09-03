/* Убрать индексы для сравнения эффективности */
/*
DROP INDEX IF EXISTS idx_group_priviledges_begin;
DROP INDEX IF EXISTS idx_group_priviledges_end;
DROP INDEX IF EXISTS idx_group_priviledges_dates;

DROP INDEX IF EXISTS idx_deposits_begin;
DROP INDEX IF EXISTS idx_deposits_end;
DROP INDEX IF EXISTS idx_deposits_dates;

DROP INDEX IF EXISTS idx_cashbacks_begin;
DROP INDEX IF EXISTS idx_cashbacks_end;
DROP INDEX IF EXISTS idx_cashbacks_dates;

DROP INDEX IF EXISTS idx_tasks_begin;
DROP INDEX IF EXISTS idx_tasks_end;
DROP INDEX IF EXISTS idx_tasks_dates;

DROP INDEX IF EXISTS trgm_idx_users_name;
DROP INDEX IF EXISTS trgm_idx_users_surname;

DROP INDEX IF EXISTS trgm_idx_banks_shortname;
DROP INDEX IF EXISTS trgm_idx_banks_fullname;
DROP INDEX IF EXISTS trgm_idx_banks_rcbic;

DROP INDEX IF EXISTS trgm_idx_tasks_name;
DROP INDEX IF EXISTS trgm_idx_tasks_description;
*/

/* Индексы для дат */
/* Привилегии групп */
CREATE INDEX idx_group_priviledges_begin ON routiner.t_group_priviledges(date_begin);
CREATE INDEX idx_group_priviledges_end ON routiner.t_group_priviledges(date_end);
CREATE INDEX idx_group_priviledges_dates ON routiner.t_group_priviledges(date_begin, date_end);

/* Депозиты */
CREATE INDEX idx_deposits_begin ON routiner.t_deposits (date_begin);
CREATE INDEX idx_deposits_end ON routiner.t_deposits (date_end);
CREATE INDEX idx_deposits_dates ON routiner.t_deposits(date_begin, date_end);

/* Кешбеки */
CREATE INDEX idx_cashbacks_begin ON routiner.t_cashbacks (date_begin);
CREATE INDEX idx_cashbacks_end ON routiner.t_cashbacks (date_end);
CREATE INDEX idx_cashbacks_dates ON routiner.t_cashbacks(date_begin, date_end);

/* Задачи */
CREATE INDEX idx_tasks_begin ON routiner.t_tasks (date_begin);
CREATE INDEX idx_tasks_end ON routiner.t_tasks (date_end);
CREATE INDEX idx_tasks_dates ON routiner.t_tasks(date_begin, date_end);

/* Полнотекстовый поиск по =, like, ilike */
CREATE EXTENSION pg_trgm;

/* Индекс по имени пользователя */
CREATE INDEX trgm_idx_users_name ON routiner.t_users USING gin (name gin_trgm_ops);
CREATE INDEX trgm_idx_users_surname ON routiner.t_users USING gin (surname gin_trgm_ops);

/* Индекс по имени банка, БИК */
CREATE INDEX trgm_idx_banks_shortname ON routiner.t_banks USING gin (short_name gin_trgm_ops);
CREATE INDEX trgm_idx_banks_fulltname ON routiner.t_banks USING gin (full_name gin_trgm_ops);
CREATE INDEX trgm_idx_banks_rcbic ON routiner.t_banks USING gin (RCBIC gin_trgm_ops);

/* Индекс по названию и описанию задачи */
CREATE INDEX trgm_idx_tasks_name ON routiner.t_tasks USING gin (name gin_trgm_ops);
CREATE INDEX trgm_idx_tasks_description ON routiner.t_tasks USING gin (description gin_trgm_ops);
