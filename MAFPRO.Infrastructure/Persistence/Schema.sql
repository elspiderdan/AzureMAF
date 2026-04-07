-- Script de creación de tablas para SQL Server
-- Proyecto: AzureMAF (MAFPRO.Infrastructure)

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Conversations]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Conversations] (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [Title] NVARCHAR(MAX) NOT NULL,
        [Status] NVARCHAR(MAX) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        CONSTRAINT [PK_Conversations] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Messages]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Messages] (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [ConversationId] UNIQUEIDENTIFIER NOT NULL,
        [Role] NVARCHAR(MAX) NOT NULL,
        [Content] NVARCHAR(MAX) NOT NULL,
        [Timestamp] DATETIME2 NOT NULL,
        CONSTRAINT [PK_Messages] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Messages_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) 
            REFERENCES [dbo].[Conversations] ([Id]) ON DELETE CASCADE
    );
END
GO

-- Índices recomendados (Entity Framework Core los generaría automáticamente para las FKs)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Messages]') AND name = N'IX_Messages_ConversationId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Messages_ConversationId] ON [dbo].[Messages] ([ConversationId]);
END
GO
