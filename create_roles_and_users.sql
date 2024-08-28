/* Создание ролей для пользователей и администраторов */
CREATE ROLE users;
CREATE ROLE admins;

/* Изымаем лишние права из public */
REVOKE CREATE ON SCHEMA public FROM public;
REVOKE ALL ON DATABASE routinerDB FROM public;

/* Настройка прав на уровне БД */
-- Даем возможность подключения
GRANT CONNECT ON DATABASE routinerDB TO users;
GRANT CONNECT ON DATABASE routinerDB TO admins;

-- Разрешаем использование схемы public
GRANT USAGE ON SCHEMA public TO users;
GRANT USAGE ON SCHEMA public TO admins;

-- Админам можно что-то создавать в базе и схеме
GRANT CREATE ON SCHEMA public TO admins;
GRANT CREATE ON DATABASE routinerDB TO admins;

/* Создадим пользователя */
DROP USER IF EXISTS veronika_nenyuk;

CREATE USER veronika_nenyuk WITH PASSWORD '****' ;

/* Выдадим роли пользователям */
GRANT admins TO veronika_nenyuk;

/* В БД RoutinerDB создаем схему */
CREATE SCHEMA routiner
    AUTHORIZATION veronika_nenyuk;