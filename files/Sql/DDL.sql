CREATE TABLE [OwnerReports] (
  [Id] INTEGER IDENTITY(1,1) NOT NULL
, [User] text NOT NULL
, [Description] text NULL
, [Date] datetime NOT NULL
, [Folder] text NOT NULL
, CONSTRAINT [PK_OwnerReports] PRIMARY KEY ([Id])
);



CREATE TABLE [OwnerReportsDetails] (
  [Id] INTEGER IDENTITY(1,1) NOT NULL
, [OwnerReportId] INTEGER NOT NULL
, [RowId] INTEGER NOT NULL
, [KeyName] text NOT NULL
, [KeyValue] text NOT NULL
, CONSTRAINT [PK_OwnerReportsDetails] PRIMARY KEY ([Id])
, FOREIGN KEY ([OwnerReportId]) REFERENCES [OwnerReports] ([id]) ON DELETE NO ACTION ON UPDATE NO ACTION
);
