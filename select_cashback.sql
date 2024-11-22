select trim(u.name || ' ' || u.surname) user_name, b.short_name bank_name, category
, TRIM(TRAILING ',' FROM TO_CHAR(rate * 100, 'FM999D99'))||'%' rate, date_begin
from routiner.t_cashbacks c 
join routiner.t_users u on u.user_id=c.user_id
join routiner.t_banks b on b.bank_id=c.bank_id
order by trim(u.name || ' ' || u.surname), b.short_name