# Tutorial
## Intro
dbpatch is a tool for database change management. The purpose of the tool is to manage database change patches and code assets. These patches and code assets can then be added to and modified in multiple source control branches, merged, and applied to a database in the proper order.

## Installing
Before using dbpatch, it must be installed on a workstation or server.

dbpatch is created using .NET 5 which is cross platform and can run on Windows, Linux, and MacOS.

### Installing on Windows
dbpatch doesn't yet have working distribution package or msi. Instead use the following instructions.

[.NET 5 runtime](https://dotnet.microsoft.com/download/dotnet/5.0) is a requirement that must be installed before dbpatch can be used.

1. Download the zip from the latest Release.
2. Right click on the zip in Explorer and open the Properties dialog. Click the checkbox to Unblock the zip and then click OK. If you do not see an Unblock checkbox near the bottom of the dialog, then click OK and go to the next step.
3. Unzip the zip file into a folder where you wish to install it. For example `C:\Program Files\dbpatch`
4. Add the folder to your PATH.

![image](unblock-zip.png)

### Installing on Linux
-> I tested on an Ubuntu docker container, WSL Ubuntu.
Need to update sudo usage in instructions.

dbpatch doesn't yet have working distribution packages but can be installed using the included install shell script, or download and view the shell script if you wish to perform the steps manually.

#### Prerequisites
* [.NET 5 runtime](https://dotnet.microsoft.com/download/dotnet/5.0)
* [wget](https://www.gnu.org/software/wget/)
* unzip

#### Steps
Using wget the install shell script can be downloaded and piped to bash to perform the install. This is the quickest way to get dbpatch installed, but it requires trusting the install script.
```
wget -qO- https://github.com/ormico/dbpatchmanager/releases/latest/download/install-dbpatch.sh | sudo bash
```
Or, you can download the install script and review it's contents before running.
```
wget -q https://github.com/ormico/dbpatchmanager/releases/latest/download/install-dbpatch.sh -O install-dbpatch.sh
#review install-dbpatch.sh before executing
cat install-dbpatch.sh
chmod +x install-dbpatch.sh
./install-dbpatch.sh
rm install-dbpatch.sh
```

If you install dbpatch to somewhere other than `/usr/local/lib/dbpatch` you may need to modify `/usr/local/lib/dbpatch/dbpatch` This file is a shell script which wraps the call to the .net command line which is used to run dbpatch.dll.

If you wish to install a version other than latest, each Release comes with an install shell script specific for that version starting with v2.1.1

### Installing on MacOS
I haven't tested this on MacOS yet, but the Linux install instructions should be similar.

## Our Example Developers
In this tutorial we are going to simulate a multi-developer team working in git and SQL Server. To simulate the team collaborating together in git we will check the changes attributed to each of our imaginary team members into a different branch. 

Our Imaginary Team:
* Ann - working on user login and privilege system
* Bettie - working on order tracking system
* Casey - initial project setup and PR review and merge

## Creating a new Project (SQL Server Type)
To get started, create a folder for the the database project. In that folder run `dbpatch init` and specify the `--dbtype` parameter. For this example specify a MS SQL Server database by using `sqlserver` as the dbtype value.
```
mkdir mydb
cd mydb
git init
dbpatch init --dbtype sqlserver
````
This will create the initial starting folders and files which include:
* A `Code` folder for stored procedures, triggers, and other executable parts of a database.
* A `Patches` folder to hold each change script.
* A `patches.json` file which holds the configuration as well as the patch dependency graph.

### Create an empty database
dbpatch requires that a database exists for it to connect to. dbpatch will not create the database for you if it does not exist.

In our example, we are using `sqlcmd` from the command line to create our example database. Execute the following from the command line.

```sqlcmd -S . -Q "create database [dbpatch-example]"```

While the above uses `sqlcmd` this can be performed with any SQL Server tool such as SQL Server Management Studio, Azure Data Studio, or VS Code.

### Setting the database Connection String
dbpatch's configution system supports splitting the configuration information into two files. In the previous step we created `patches.json` using the `init` command. In this step, we are going to create the second file `patches.local.json`. Where `patches.json` is checked into source control, `patches.local.json` typically is not. The local file, is where workstation or server specific settings are specified. When dbpatch runs, these two configuration files are merged with the local file overriding any settings made in `patches.json`. While you can override any setting, typically the `ConnectionString` is the most common setting used in the local file.

Using a text editor, create a file in the database project folder named `patches.local.json`. The contents of the file should look like the following, but in place of this connection string specify the Connection String to your development database. This is usually a database and server running on your local workstation.

```
{
    "ConnectionString": "Server=.;Database=dbpatch-example;Trusted_Connection=True;"
}
```

### Ignoring patches.local.json and checking in changes in source control

```
echo "patches.local.json" > .gitignore
git stage *
git commit -m first
```

git commit output:
//todo: update example w/o testdb & correct patch name
```
[main (root-commit) 7d7f6e3] first
 8 files changed, 77 insertions(+)
 create mode 100644 .gitignore
 create mode 100644 Patches/202103082324-1408-p2/p.sql
 create mode 100644 Patches/202103082324-2009-p1/p.sql
 create mode 100644 Patches/202103082324-7288-p3/p.sql
 create mode 100644 Patches/202103082324-9737-p4/p.sql
 create mode 100644 patches.json
 create mode 100644 sqltools_20210308232445_31788.log
 create mode 100644 test.db
 ```

## Creating the first Patches

```dbpatch addpatch -n first-patch```

```202103082324-1408-first-patch```
```
git branch sue/db-changes
git checkout sue/db-changes
git stage *
git commit -m "first patch"
```


//todo: decide what kind of db we are building and create the final whole project in an example repo or folder. make a schema diagram and post it in this example

### Building the Database after Schema additions

//todo: make sure dbpatch doesn't do anything if patches.json is not in current folder
```dbpatch build```

## Merging schema changes from another user
### Building the Database after Schema merge

## Examining InstalledPatches table
//todo: add order by installed date column and show query results
```sqlcmd -S . -d dbpatch-example -Q "select * from installedpatches"```

## Creating the first Database Code entries
### Stored Procedures
### User Defined Functions
### User Defined Function with Dependencies
### Triggers
### Building the Database after Code additions

## Merging Code entries
### Building the Database after Code merge

# Deploying changes to production

# Conclusion
