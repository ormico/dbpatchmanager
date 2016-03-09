IF (NOT EXISTS (SELECT * 
                 FROM INFORMATION_SCHEMA.TABLES 
                 WHERE TABLE_SCHEMA = 'dbo'
                 AND  TABLE_NAME = 'InstalledPatches'))
BEGIN
    --TODO: create in new schema
    CREATE TABLE dbo.InstalledPatches
    (
        PatchId VARCHAR(50) NOT NULL,
        CONSTRAINT PK_InstalledPatches PRIMARY KEY CLUSTERED 
        (
            PatchId ASC
        ),
        InstalledDate DATETIME NOT NULL DEFAULT GETDATE()
    )
END
