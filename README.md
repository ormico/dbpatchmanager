# DB Patch Manager
Database development tool for change control.
v2.0 has been updated to .NET Core for cross platform support.

## Create new db project
```PS C:\MyProject> .\dbpatch init --dbtype "Ormico.DbPatchManager.SqlServer.dll, Ormico.DbPatchManager.SqlServer.SqlDatabase"```

This will create a new project file named `patches.json` and initilize it to the SQL Server plugin.

Create a new file named `patches.local.json` When you are a adding files to source control, do not add this file. Open this file in a text editor and add your connecton string something like:

```
{
    "ConnectionString": "Server=.;Database=TestDatabase;Trusted_Connection=True;"
}
```

Each developer would enter their local connection string. When deploying, you would enter the production server's connection string.

## Add a database patch
```PS C:\MyProject> .\dbpatch addpatch --name TestPatch```

Creates a folder for the patch in `C:\MyProject\Patches\` and adds the patch to the patches.json file. The folder is named using a date time string and a random number and the name. For example something like `201708011412-2403-testpatch`. User can place .sql files in the patch folder and they will be run when the patch is applied. If the user includes more than one patch file, they are run in alphabetical order.

## Add a database code item
Code items are database items that are applied on each build instead of only once like patches. Typically code items are Stored Procedures, Functions, Views, and Triggers.

To add a new stored procedure create a file in the `code` folder. The new file can be named whatever you want and follow whatever naming scheme you want, but the file extension must the code file type. So a stored procedure must file extension must be .sproc.sql For example, you could name it myFunct.sproc.sql

By default, you can also use the file extensions .sproc2.sql or .sproc3.sql if other stored procedures depends on other stored procedures and you want to make sure those stored procedures load first. 

The default list of code file extensions and the order they load is:
* .view.sql - View
* .udf.sql - User Defined Function
* .view2.sql - View
* .udf2.sql - User Defined Function
* .view3.sql - View
* .udf3.sql - User Defined Function
* .sproc.sql - Stored Procedure
* .sproc2.sql - Stored Procedure
* .sproc3.sql - Stored Procedure
* .trigger.sql - Trigger
* .trigger2.sql - Trigger
* .trigger3.sql - Trigger

## Build Database
```PS C:\MyProject> .\dbpatch build```

Applies all missing patches and runs all code files.
