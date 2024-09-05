with xmldata as (select 
				 pg_read_file('D:\\RoutinerProject\\20240830_ED807_full.xml')::xml 
				 as xml_data),
	parsed as  (select BIC, NameP, PrntBIC, PtType, Tnp || '. ' || Nnp || ', ' || Adr Address, xmlelement(name acc, Accounts) acc
				from xmldata, XMLTABLE('//ED807/BICDirectoryEntry'
                PASSING xml_data
                COLUMNS bic text PATH '@BIC',
					    NameP text PATH 'ParticipantInfo/@NameP',
					    PrntBIC text PATH 'ParticipantInfo/@PrntBIC',
					    PtType int PATH 'ParticipantInfo/@PtType',
						Tnp text PATH 'ParticipantInfo/@Tnp',
						Nnp text PATH 'ParticipantInfo/@Nnp',	
						Adr text PATH 'ParticipantInfo/@Adr',
					    Accounts xml PATH 'Accounts'
					  ) p),
  allinfo as (
	select BIC, NameP, PrntBIC, PtType, Address, 
	unnest(xpath('//acc/Accounts/@Account', acc))::text Account,
	unnest(xpath('//acc/Accounts/@AccountCBRBIC', acc))::text AccountCBRBIC,
	unnest(xpath('//acc/Accounts/@RegulationAccountType', acc))::text RegulationAccountType,
	to_date(unnest(xpath('//acc/Accounts/@DateIn', acc))::text, 'YYYY-MM-DD') AccountDateIn
	from parsed
	 where PtType != 90 -- не закрыт
  )
  select BIC, NameP, PrntBIC, PtType, Address, Account, AccountCBRBIC, RegulationAccountType, AccountDateIn
  from allinfo
  where regexp_like(namep, 'сбер|азиатск|альфа|втб|дальневосточный|почта|мтс|открытие', 'i')
 and prntbic is null -- главный филиал
 and RegulationAccountType='CRSA' -- корсчет
 --and parsed.bic='200000548'
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

