with xmldata as (select 
				 pg_read_file('D:\\RoutinerProject\\20240830_ED807_full.xml')::xml 
				 as xml_data),
	parsed as  (select bic, NameP, PrntBIC, PtType, Accounts, Tnp || '. ' || Nnp || ', ' || Adr Address, xmlelement(name acc, Accounts) acc
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
					  ) p) 		 
select  parsed.bic, namep, prntbic,
(xpath('//acc/Accounts/@Account', acc)) Account,
(xpath('//acc/Accounts/@AccountCBRBIC', acc)) AccountCBRBIC,
 Address, 
(xpath('//acc/Accounts/@DateIn', acc)) AccountDateIn
from parsed
 where PtType != 90
 and regexp_like(namep, 'сбер|азиатск|альфа|втб|дальневосточный|почта|мтс', 'i')
 and prntbic is null -- главный филиал
 --and parsed.bic='200000548'
;

