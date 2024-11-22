create or replace function routiner.user_status(v_chat_id bigint, v_telegram_account text)
returns int as $$
begin
  -- Если такая таблица с логами есть, запрашиваем
  if exists (SELECT FROM routiner.t_users WHERE chat_id = v_chat_id AND is_active) then
  	return 1; -- Текущий чат
  elsif exists (SELECT FROM routiner.t_users WHERE telegram_account ilike v_telegram_account AND is_active) then
  	return 2; -- Имеется с таким юзернеймом 
  else
    return 0; -- Нет такого юзера
  end if;
end;
$$ language plpgsql;