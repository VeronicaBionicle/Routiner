with xmldata as (select 
				 pg_read_file('D:\\RoutinerProject\\20240830_ED807_full.xml')::xml 
				 as xml_data),
	parsed as  (select BIC, NameP, PrntBIC, PtType, ParticipantStatus
				, case when Ind is not null then Ind || ', ' else '' end || Tnp || '. ' || Nnp || ', ' || Adr Address
				, xmlelement(name acc, Accounts) acc
				from xmldata, XMLTABLE('//ED807/BICDirectoryEntry'
                PASSING xml_data
                COLUMNS bic text PATH '@BIC',
					    NameP text PATH 'ParticipantInfo/@NameP',
					    PrntBIC text PATH 'ParticipantInfo/@PrntBIC',
					    PtType int PATH 'ParticipantInfo/@PtType',
						Ind text PATH 'ParticipantInfo/@Ind',
						Tnp text PATH 'ParticipantInfo/@Tnp',
						Nnp text PATH 'ParticipantInfo/@Nnp',	
						Adr text PATH 'ParticipantInfo/@Adr',
						ParticipantStatus text PATH 'ParticipantInfo/@ParticipantStatus',
					    Accounts xml PATH 'Accounts'
					  ) p),
  allinfo as (
	select BIC, NameP, PrntBIC, PtType, Address, ParticipantStatus,
	unnest(xpath('//acc/Accounts/@Account', acc))::text Account,
	unnest(xpath('//acc/Accounts/@AccountCBRBIC', acc))::text AccountCBRBIC,
	unnest(xpath('//acc/Accounts/@RegulationAccountType', acc))::text RegulationAccountType,
	to_date(unnest(xpath('//acc/Accounts/@DateIn', acc))::text, 'YYYY-MM-DD') AccountDateIn,
	unnest(xpath('//acc/Accounts/@AccountStatus', acc))::text AccountStatus
	from parsed
  )
  select BIC, NameP, Address, Account, AccountCBRBIC, AccountDateIn
  from allinfo
  where regexp_like(namep, 'сбер|азиатск|альфа|втб|дальневосточный|почта|мтс', 'i')
-- and prntbic is null -- главный филиал = null
 and RegulationAccountType='CRSA' -- корсчет
 and PtType in (20) -- Кредитная организация
 and AccountStatus != 'ACDL' -- не удаляется
 and ParticipantStatus != 'PSDL' -- не удаляется
 --and bic='200000548'
;

/*
case RegulationAccountType
		when 'CBRA' then 'Счёт Банка России'
        when 'CRSA' then 'Корреспондентский счёт'
        when 'BANA' then 'Банковский счёт'
        when 'TRSA' then 'Счёт Федерального казначейства'
        when 'TRUA' then 'Счёт доверительного управления'
        when 'CLAC' then 'Клиринговый счёт'
        when 'UTRA' then 'Единый казначейский счёт'
		else RegulationAccountType end
*/

/* case PtType
when 00 then 'Главное управление Банка России'
when 10 then 'Расчетно-кассовый центр'
when 12 then 'Отделение, отделение – национальный банк главного управления Банка России'
when 15 then 'Структурное подразделение центрального аппарата Банка России'
when 16 then 'Кассовый центр'
when 20 then 'Кредитная организация'
when 30 then 'Филиал кредитной организации'
when 40 then 'Полевое учреждение Банка России'
when 51 then 'Федеральное казначейство'
when 52 then 'Территориальный орган Федерального казначейства'
when 60 then 'Иностранный банк (иностранная кредитная организация)'
when 65 then 'Иностранный центральный (национальный) банк'
when 71 then 'Клиент кредитной организации, являющийся косвенным участником'
when 75 then 'Клиринговая организация'
when 78 then 'Внешняя платежная система'
when 90 then 'Конкурсный управляющий (ликвидатор, ликвидационная комиссия)'
when 99 then 'Клиент Банка России, не являющийся участником платежной системы'
else PtType::text end
*/
