declare @schemas xml, @defaultschemas xml = '<schemas><s v="dbo"/></schemas>'
set @schemas = @x
--select @defaultschemas

--<schemas>
--	<s v="dbo"/>
--	<s v="api"/>
--</schemas>
select
	[schemas].s.value('@v', 'VARCHAR(MAX)') v
from @schemas.nodes('/schemas/s') as [schemas](s)

