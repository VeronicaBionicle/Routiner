insert into routiner.t_users (name, surname, telegram_account, is_active)
values ('Вероника','Ненюк','VeronicaNenyuk',true);

select * from routiner.t_users where name = 'Вероника' and surname = 'Ненюк';

select * from routiner.t_banks
where regexp_like(short_name, 'сбер|азиатск|альфа|втб', 'i');

insert into routiner.t_cashbacks (category, rate, date_begin, date_end, user_id, bank_id)
-- сбер
select 'Аптеки' category, 0.05 rate
, to_date('01.09.2024','DD.MM.YYYY') date_begin
, to_date('30.09.2024','DD.MM.YYYY') date_end
, (select user_id from routiner.t_users where name = 'Вероника' and surname = 'Ненюк') user_id
, (select bank_id from routiner.t_banks where regexp_like(short_name, 'сбер', 'i')) bank_id
union all
select 'Такси и каршеринг' category, 0.05 rate
, to_date('01.09.2024','DD.MM.YYYY') date_begin
, to_date('30.09.2024','DD.MM.YYYY') date_end
, (select user_id from routiner.t_users where name = 'Вероника' and surname = 'Ненюк') user_id
, (select bank_id from routiner.t_banks where regexp_like(short_name, 'сбер', 'i')) bank_id
union all
select 'Товары для дома' category, 0.02 rate
, to_date('01.09.2024','DD.MM.YYYY') date_begin
, to_date('30.09.2024','DD.MM.YYYY') date_end
, (select user_id from routiner.t_users where name = 'Вероника' and surname = 'Ненюк') user_id
, (select bank_id from routiner.t_banks where regexp_like(short_name, 'сбер', 'i')) bank_id

 -- альфа
 union all
 select 'На все' category, 0.01 rate
, to_date('01.09.2024','DD.MM.YYYY') date_begin
, to_date('30.09.2024','DD.MM.YYYY') date_end
, (select user_id from routiner.t_users where name = 'Вероника' and surname = 'Ненюк') user_id
, (select bank_id from routiner.t_banks where regexp_like(short_name, 'альфа', 'i')) bank_id
union all
select 'Техника' category, 0.05 rate
, to_date('01.09.2024','DD.MM.YYYY') date_begin
, to_date('30.09.2024','DD.MM.YYYY') date_end
, (select user_id from routiner.t_users where name = 'Вероника' and surname = 'Ненюк') user_id
, (select bank_id from routiner.t_banks where regexp_like(short_name, 'альфа', 'i')) bank_id
union all
select 'Активный отдых' category, 0.05 rate
, to_date('01.09.2024','DD.MM.YYYY') date_begin
, to_date('30.09.2024','DD.MM.YYYY') date_end
, (select user_id from routiner.t_users where name = 'Вероника' and surname = 'Ненюк') user_id
, (select bank_id from routiner.t_banks where regexp_like(short_name, 'альфа', 'i')) bank_id
union all
select 'Спортивные товары' category, 0.07 rate
, to_date('01.09.2024','DD.MM.YYYY') date_begin
, to_date('30.09.2024','DD.MM.YYYY') date_end
, (select user_id from routiner.t_users where name = 'Вероника' and surname = 'Ненюк') user_id
, (select bank_id from routiner.t_banks where regexp_like(short_name, 'альфа', 'i')) bank_id

-- втб
union all
select 'Электроника' category, 0.04 rate
, to_date('01.09.2024','DD.MM.YYYY') date_begin
, to_date('30.09.2024','DD.MM.YYYY') date_end
, (select user_id from routiner.t_users where name = 'Вероника' and surname = 'Ненюк') user_id
, (select bank_id from routiner.t_banks where regexp_like(short_name, 'втб', 'i')) bank_id
union all
select 'Дом и ремонт' category, 0.03 rate
, to_date('01.09.2024','DD.MM.YYYY') date_begin
, to_date('30.09.2024','DD.MM.YYYY') date_end
, (select user_id from routiner.t_users where name = 'Вероника' and surname = 'Ненюк') user_id
, (select bank_id from routiner.t_banks where regexp_like(short_name, 'втб', 'i')) bank_id
union all
select 'Супермаркеты' category, 0.015 rate
, to_date('01.09.2024','DD.MM.YYYY') date_begin
, to_date('30.09.2024','DD.MM.YYYY') date_end
, (select user_id from routiner.t_users where name = 'Вероника' and surname = 'Ненюк') user_id
, (select bank_id from routiner.t_banks where regexp_like(short_name, 'втб', 'i')) bank_id