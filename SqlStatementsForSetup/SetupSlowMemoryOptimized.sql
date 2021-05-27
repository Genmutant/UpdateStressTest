IF NOT EXISTS(SELECT 1 FROM sys.databases WHERE name='TestDbSlowInMemory')
BEGIN
    CREATE DATABASE [TestDbSlowInMemory]
        ON  PRIMARY
        ( NAME = N'TestDbSlowInMemory', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL14.MSSQLSERVER\MSSQL\DATA\TestDbSlowInMemory'  ),
        FILEGROUP [TestDbSlowInMemory] CONTAINS MEMORY_OPTIMIZED_DATA DEFAULT
        ( NAME = N'TestDbSlowInMemoryInMemory', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL14.MSSQLSERVER\MSSQL\DATA\TestDbSlowInMemoryInMemory' )
        LOG ON
        ( NAME = N'TestDbSlowInMemory_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL14.MSSQLSERVER\MSSQL\DATA\TestDbSlowInMemory_log'  )
GO

ALTER DATABASE [TestDbSlowInMemory] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [TestDbSlowInMemory] SET AUTO_UPDATE_STATISTICS_ASYNC ON 
GO
ALTER DATABASE [TestDbSlowInMemory] SET ALLOW_SNAPSHOT_ISOLATION ON 
GO
ALTER DATABASE [TestDbSlowInMemory] SET READ_COMMITTED_SNAPSHOT ON 
GO
ALTER DATABASE [TestDbSlowInMemory] SET RECOVERY SIMPLE 
GO
ALTER DATABASE [TestDbSlowInMemory] SET MULTI_USER 
GO
ALTER DATABASE [TestDbSlowInMemory] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [TestDbSlowInMemory] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [TestDbSlowInMemory] SET DELAYED_DURABILITY = DISABLED 
GO
USE [TestDbSlowInMemory]
GO
ALTER DATABASE SCOPED CONFIGURATION SET QUERY_OPTIMIZER_HOTFIXES = ON;
GO
ALTER DATABASE [TestDbSlowInMemory] SET  READ_WRITE 
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

PRINT('Setup Table...')
DROP TABLE IF EXISTS [dbo].[ContainerAutoTest]

CREATE TABLE [dbo].[ContainerAutoTest]
(
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ResourceId] [int] NULL,
	[CurrentStepId] [int] NULL,
	[ProductId] [int] NULL,
	[Level] [nvarchar](50) NULL,
	[LastComment] [nvarchar](100) NULL,
	[SysStart] [datetime2](7) NULL,
	[Name] NVARCHAR(100) NOT NULL,
	
    INDEX [Ix_CurrentStep] NONCLUSTERED([CurrentStepId] ASC),
    INDEX [Ix_Level] NONCLUSTERED([Level] ASC),
    INDEX [Ix_Product] NONCLUSTERED([ProductId] ASC),
    INDEX [Ix_Resource] NONCLUSTERED([ResourceId] ASC),
    PRIMARY KEY NONCLUSTERED ([Id]),
    UNIQUE NONCLUSTERED ([Name])
)
WITH ( MEMORY_OPTIMIZED = ON , DURABILITY = SCHEMA_AND_DATA )
GO

ALTER TABLE [dbo].[ContainerAutoTest] ADD  DEFAULT ('Modul') FOR [Level]
GO

ALTER TABLE [dbo].[ContainerAutoTest] ADD  DEFAULT (SYSUTCDATETIME()) FOR [SysStart]
GO

PRINT('Clearing table...')
DELETE FROM [dbo].[ContainerAutoTest] WHERE 1=1
GO
PRINT('Filling table...')
DECLARE @RowsToInsert INT = 10;
DECLARE @RowsInserted INT = 0;
WHILE @RowsInserted < @RowsToInsert
BEGIN
    INSERT INTO ContainerAutoTest (Name)
        SELECT TOP 1000000 NEWID()
            FROM sys.all_columns ac1
                CROSS JOIN sys.all_columns ac2;
    SET @RowsInserted = @RowsInserted + 1;
END
PRINT('Filled table')
GO

