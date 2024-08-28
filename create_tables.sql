/* Пользователи */
CREATE TABLE routiner.t_users
( 
	user_id int GENERATED ALWAYS AS IDENTITY NOT NULL, 
	name text NOT NULL,
	surname text,
	telegram_account text,
	is_active boolean DEFAULT true, -- для отключения, а не удаления записи
	CONSTRAINT PK_users_user_id PRIMARY KEY(user_id)
);
ALTER TABLE IF EXISTS routiner.t_users OWNER to veronika_nenyuk;

/* Группы пользователей */
CREATE TABLE routiner.t_groups
(
	group_id int GENERATED ALWAYS AS IDENTITY NOT NULL, 
	name text NOT NULL,
	CONSTRAINT PK_groups_group_id PRIMARY KEY(group_id)
);
ALTER TABLE IF EXISTS routiner.t_groups OWNER to veronika_nenyuk;

/* Привилегии групп */
CREATE TABLE routiner.t_group_priviledges
(
	priviledge_id int GENERATED ALWAYS AS IDENTITY NOT NULL, 
	group_id int,

	can_watch_group boolean DEFAULT true,
	can_change_group boolean DEFAULT false,
	can_change_all boolean DEFAULT false,
	can_moderate_users boolean DEFAULT false,

	date_begin TIMESTAMPTZ,
	date_end TIMESTAMPTZ, 
	CONSTRAINT PK_group_priviledges_priviledge_id PRIMARY KEY(priviledge_id),
	CONSTRAINT FK_group_priviledges_group_id FOREIGN KEY(group_id) REFERENCES routiner.t_groups(group_id)
);
-- Admins и Priviledged - отдельные группы для выдачи дополнительных прав пользователю
ALTER TABLE IF EXISTS routiner.t_group_priviledges OWNER to veronika_nenyuk;

/* Таблица связей пользователей и групп */
CREATE TABLE routiner.t_user_group
(
	user_group_id int GENERATED ALWAYS AS IDENTITY NOT NULL, 
	user_id int,
	group_id int,
	CONSTRAINT PK_user_group_user_group_id PRIMARY KEY(user_group_id),
	CONSTRAINT FK_user_group_user_id FOREIGN KEY(user_id) REFERENCES routiner.t_users(user_id),
	CONSTRAINT FK_user_group_group_id FOREIGN KEY(group_id) REFERENCES routiner.t_groups(group_id)
);
ALTER TABLE IF EXISTS routiner.t_user_group OWNER to veronika_nenyuk;

/* Банки */
CREATE TABLE routiner.t_banks
( 
	bank_id int GENERATED ALWAYS AS IDENTITY NOT NULL, 
	short_name text NOT NULL,
	full_name text,
	TIN text, -- ИНН (taxpayer identification number)
	RRC text, -- КПП (registration reason code)
	RCBIC text, -- БИК (Russian Central Bank Identifier Code)
	correspondent_account text, -- корреспонденский счёт
	CONSTRAINT PK_banks_bank_id PRIMARY KEY(bank_id)
);
ALTER TABLE IF EXISTS routiner.t_banks OWNER to veronika_nenyuk;

/* Вклады */
CREATE TABLE routiner.t_deposits
(
	deposit_id int GENERATED ALWAYS AS IDENTITY NOT NULL, 
	name text,
	amount decimal NOT NULL, -- Сумма депозита/вклада
	rate decimal NOT NULL, -- Ставка
	date_begin date NOT NULL, -- Дата открытия вклада
	period interval NOT NULL, -- Срок вклада
	date_end date,   -- Фактическая дата закрытия вклада
	contract_number text, -- Номер договора

	is_replenishable boolean,	-- Разрешено пополнение
	is_withdrawable boolean, -- Разрешено снятие
	is_renewable boolean, -- Пролонгируемый

	user_id int, -- Владелец депозита
	bank_id int, -- Банк депозита
	previous_deposit_id int, -- Если вклад был пролонгирован - id предыдущего

	CONSTRAINT PK_deposits_deposit_id PRIMARY KEY(deposit_id),
	CONSTRAINT FK_deposits_user_id FOREIGN KEY(user_id) REFERENCES routiner.t_users(user_id),
	CONSTRAINT FK_deposits_bank_id FOREIGN KEY(bank_id) REFERENCES routiner.t_banks(bank_id),
	CONSTRAINT FK_deposits_previous_deposit_id FOREIGN KEY(previous_deposit_id) REFERENCES routiner.t_deposits(deposit_id)
);
ALTER TABLE IF EXISTS routiner.t_deposits OWNER to veronika_nenyuk;

/* Кешбеки */
CREATE TABLE routiner.t_cashbacks
(
	cashback_id int GENERATED ALWAYS AS IDENTITY NOT NULL, 
	category text NOT NULL, -- Категория кешбека
	rate decimal NOT NULL, -- Процент
	date_begin date NOT NULL, -- Дата начала действия кешбека
	date_end date,   -- Дата окончания действия кешбека

	user_id int, -- Владелец кешбека
	bank_id int, -- Банк кешбека

	CONSTRAINT PK_cashbacks_cashback_id PRIMARY KEY(cashback_id),
	CONSTRAINT FK_cashbacks_user_id FOREIGN KEY(user_id) REFERENCES routiner.t_users(user_id),
	CONSTRAINT FK_cashbacks_bank_id FOREIGN KEY(bank_id) REFERENCES routiner.t_banks(bank_id)
);
ALTER TABLE IF EXISTS routiner.t_cashbacks OWNER to veronika_nenyuk;

/* Задачи/напоминания */
CREATE TABLE routiner.t_tasks
(
	task_id int GENERATED ALWAYS AS IDENTITY NOT NULL, 
	name text NOT NULL,
	description text,
	
	date_begin TIMESTAMPTZ,
	date_end TIMESTAMPTZ, -- Для единоразовых задач будет равен началу ?
	period interval,
	period_before interval, -- Период для напоминания за n времени до даты задачи

	task_type char, -- P - periodical, O - once
	user_id int,
	CONSTRAINT PK_tasks_task_id PRIMARY KEY(task_id),
	CONSTRAINT FK_tasks_user_id FOREIGN KEY(user_id) REFERENCES routiner.t_users(user_id)
);
ALTER TABLE IF EXISTS routiner.t_tasks OWNER to veronika_nenyuk;

/* Таблицы для логгирования изменений? */