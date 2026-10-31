![rect-logo-small](https://user-images.githubusercontent.com/1731364/110421365-60dec400-806b-11eb-9a53-d783ca935042.png)
# DB Patch Manager
Database development tool for change control.

v2 has been updated to .NET Core for cross platform support.

DB Patch Manager supports database development across multiple source control branches by tracking the dependencies of each patch and installing them in the correct order. When a developer adds a new patch, that patch depends on all the previous patches recorded in that branch. When branches are merged the `patches.json` file is merged to include all patches and the dependency tree from each branch. 

## Installing
### Windows
dbpatch doesn't yet have working distribution package or an msi.

1. Download the zip from the latest Release.
2. Right click on the zip in Explorer and open the Properties dialog. Click the checkbox to Unblock the zip and then click OK. If you do not see an Unblock checkbox near the bottom of the dialog, then click OK and go to the next step.
3. Unzip the zip file into a folder where you wish to install it. For example `C:\Program Files\dbpatch`
4. Add the folder to your PATH.

![image](docs/unblock-zip.png)

### Linux
dbpatch doesn't yet have working distribution packages but can be installed using the included install shell script, or download and view the shell script if you wish to perform the steps manually.

### Prerequisites
* .NET 5
* wget
* unzip

```
wget -qO- https://github.com/ormico/dbpatchmanager/releases/latest/download/install-dbpatch.sh | bash
```

or

```
wget -q https://github.com/ormico/dbpatchmanager/releases/latest/download/install-dbpatch.sh -O install-dbpatch.sh
chmod +x install-dbpatch.sh
./install-dbpatch.sh
rm install-dbpatch.sh
```

If you install dbpatch to somewhere other than `/usr/local/lib/dbpatch` you may need to modify `/usr/local/lib/dbpatch/dbpatch` This file is a shell script which wraps the call to the .net commandline which is used to run dbpatch.dll.

If you wish to install a version other than latest, each Release comes with an install shell script specific for that version starting with v2.1.1

## Create new db project
```MyProject> dbpatch init --dbtype sqlserver```

This will create a new project file named `patches.json` and initilize it to the SQL Server plugin.

Create a new file named `patches.local.json` When you are a adding files to source control, do not add this file. Open this file in a text editor and add your connecton string something like:

```
{
    "ConnectionString": "Server=.;Database=TestDatabase;Trusted_Connection=True;"
}
```

Each developer would enter their local connection string. When deploying, you would enter the production server's connection string.

## Add a database patch
```MyProject> dbpatch addpatch -n TestPatch```

Creates a folder for the patch in `...\MyProject\Patches\` and adds the patch to the patches.json file. The folder is named using a date time string and a random number and the name. For example something like `201708011412-2403-testpatch`. User can place .sql files in the patch folder and they will be run when the patch is applied. If the user includes more than one patch file, they are run in alphabetical order.

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
```MyProject> dbpatch build```

Applies all missing patches and runs all code files.

### Empty and comment-only SQL files
A `.sql` file in a patch or code folder that contains no executable SQL is skipped with a warning instead of being sent to the database. This covers files that are:
* empty
* whitespace only, including invisible characters such as a byte order mark or zero-width space
* only empty statements (`;`)
* only `--` line comments and `/* */` block comments, or a mix of these

A patch made up entirely of skipped files is still recorded as installed.

Detection is deliberately conservative, so a file with real SQL is never skipped. Anything else is sent to the database plugin unchanged, and how it is handled depends on the database driver. Examples:
* `# comment` (a comment in MySQL, a syntax error in SQL Server)
* `/*! ... */` or `/*M! ... */` (executable comments in MySQL and MariaDB)
* nested `/* /* */ */` or unterminated block comments

How ODBC drivers handle these when the file is sent to the database, tested with SQL Server ODBC Driver 17, ODBC Driver 18 and the legacy "SQL Server" driver, and MySQL ODBC 9.5 against MySQL 8.4. dbpatch skips every row marked *skipped*, so it works the same way on any driver:

| File content | dbpatch | SQL Server | MySQL |
|---|---|---|---|
| empty | *skipped* | rejected by .NET before reaching the driver: `CommandText property has not been initialized` | same |
| whitespace only | *skipped* | accepted | `Query was empty` (1065) |
| `;` only | *skipped* | accepted | `Query was empty` (1065) |
| `--` or `/* */` comments only | *skipped* | accepted | accepted |
| zero-width space or extra byte order mark | *skipped* | byte order mark: syntax error | syntax error (1064) |
| `# comment` | sent | syntax error (102) | accepted |

If a driver rejects a file that dbpatch did not skip, edit the file so it either contains executable SQL or matches the skipped patterns above.

### Script overrides
dbpatch looks for override scripts in a `ScriptOverrides` folder in the current directory: `InitPatchTable.sql`, `AddInstalledPatch.sql` and `GetInstalledPatches.sql`. Whether they are used depends on the plugin. The SQL Server plugin uses its own built-in scripts and ignores them. The ODBC plugin requires all three. With ODBC, a missing, empty or comment-only override stops the build with an error naming the file. Skipping `AddInstalledPatch.sql`, for example, would mean patches are never recorded as installed and would run again on every build.

### When a script fails
The build stops at the first file that fails, and the error names the file and patch. Patches are not run inside a transaction. Patches installed earlier in the same build stay installed. The failing patch is not recorded as installed but may be partly applied, depending on the database. For example:
* SQL Server checks the whole script for syntax errors first, so a syntax error means nothing in that file ran.
* MySQL with the ODBC `MULTI_STATEMENTS=1` option runs statements one at a time, so statements before the failing one have already run.

Fix the file and run `dbpatch build` again. If the patch was partly applied, undo the partial change or make the script safe to re-run first.

### Retiring a patch that has already been deployed
If a bug is found in a patch that has already been applied to some databases, don't delete the patch. Instead:
1. Replace the contents of its `.sql` files with a comment explaining why, such as `-- removed: replaced by patch 202410250000-0001-fix-orders`.
2. Add a new patch with the corrected change.

Databases that already ran the old patch skip it and apply the new one. New databases record the old patch as installed without running anything, then apply the new one.

## Database Plugin
[SQL Server Plugin](https://github.com/ormico/dbpatchmanager-sqlserver)
