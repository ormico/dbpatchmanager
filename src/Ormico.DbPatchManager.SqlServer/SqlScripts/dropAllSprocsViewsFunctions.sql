set nocount on;

declare @schemas xml
declare @defaultschemas xml = '<schemas><s v="dbo"/></schemas>'

if @schemas is null
    set @schemas = @defaultschemas

declare @objecttypes xml
declare @defaultobjecttypes xml = '<types><t v="AF"/><t v="FN"/><t v="IF"/><t v="P"/><t v="RF"/><t v="TF"/><t v="TR"/><t v="V"/><t v="X"/></types>'

if @objecttypes is null
    set @objecttypes = @defaultobjecttypes

declare @schemastbl table
(
    schema_name nvarchar(max)
)

--<schemas>
--	<s v="dbo"/>
--	<s v="api"/>
--</schemas>
insert into @schemastbl(schema_name)
select [schemas].s.value('@v', 'VARCHAR(MAX)') schema_name
from @schemas.nodes('/schemas/s') as [schemas](s)

declare @objecttypestbl table
(
    [type] char(2)
)

--<types>
--	<t v="P"/>
--	<t v="FN"/>
--	<t v="TF"/>
--</types>
insert into @objecttypestbl([type])
select [types].t.value('@v', 'CHAR(2)') [type]
from @objecttypes.nodes('/types/t') as [types](t)

/*
http://msdn.microsoft.com/en-us/library/ms190324.aspx
AF = Aggregate function (CLR)
C = CHECK constraint
D = DEFAULT (constraint or stand-alone)
F = FOREIGN KEY constraint
FN = SQL scalar function
FS = Assembly (CLR) scalar-function
FT = Assembly (CLR) table-valued function
IF = SQL inline table-valued function
IT = Internal table
P = SQL Stored Procedure
PC = Assembly (CLR) stored-procedure
PG = Plan guide
PK = PRIMARY KEY constraint
R = Rule (old-style, stand-alone)
RF = Replication-filter-procedure
S = System base table
SN = Synonym
SQ = Service queue
TA = Assembly (CLR) DML trigger
TF = SQL table-valued-function
TR = SQL DML trigger 
TT = Table type
U = Table (user-defined)
UQ = UNIQUE constraint
V = View
X = Extended stored procedure
*/

-- drop all user defined code objects in schemas defined in @schemas
declare @name varchar(256)
declare @type varchar(4)
declare @sql nvarchar(max)
declare @curSchemaProcs cursor

set @curSchemaProcs = cursor for 
    select '[' + s.[name] + '].[' + O.[name] + ']' proc_name, O.[type]
    from sys.objects O left outer join 
        sys.extended_properties E ON O.object_id = E.major_id 
        inner join sys.schemas s on O.schema_id = s.schema_id
    where s.[name] in (select cast(schema_name as sysname) [name] from @schemastbl)
        AND O.name is not null 
        --checking is_mis_shipped and the extended property microsoft_database_tools_support
        --allows us to ignore MS system stored procedures
        and ISNULL(O.is_ms_shipped, 0) = 0 
        and ISNULL(E.name, '') <> 'microsoft_database_tools_support' 
        and O.type COLLATE DATABASE_DEFAULT
            in (select [type] from @objecttypestbl)
    order by O.name

open @curSchemaProcs

fetch @curSchemaProcs into @name, @type

while(@@FETCH_STATUS = 0)
begin
    set @sql = N'drop ' + 
        case @type
            when 'AF' then N'AGGREGATE'
            when 'FN' then N'function'
            when 'FS' then N'function'
            when 'FT' then N'function'
            when 'IF' then N'function'
            when 'P' then N'procedure'
            when 'PC' then N'procedure'
            when 'RF' then N'procedure'
            when 'TA' then N'trigger'
            when 'TF' then N'function'
            when 'TR' then N'trigger'
            when 'V' then N'view'
            when 'X' then N'procedure'
        end
        + N' ' + @name
    print @sql
    execute (@sql);

    fetch @curSchemaProcs into @name, @type    
end 

close @curSchemaProcs
deallocate @curSchemaProcs
