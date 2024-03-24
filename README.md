This README is just a fast quick start document. You can find more detailed information in the official docs.

## What is FolderSecurityViewer?

FolderSecurityViewer is an NTFS permissions reporter that helps you analyze your Windows folders or network shares and report all NTFS permissions' owners. Tracing down users' effective NTFS permissions creates visibility into your data's effective permissions and access rights. It gives insights into whether rights assignments are made for a group or direct assignment.


### Fast object traversal and break-down

Verifying user permissions for folders can be done quickly. There is no need to hassle with nested Active Directory security groups or time-consuming manual research for all the Active Directory groups to find the rights assignment of a folder.

The NTFS Permissions Reporter traverses nested Active Directory groups while analyzing effective NTFS permissions owners. It also examines all group members and recursively determines what users or groups exist there.


### Exclude certain groups and users from the analysis

Suppose you have special groups for Operators or Backup users and want to exclude them from your NTFS permissions report. In that case, you can add them to the Excluded Groups section in the configuration. The Active Directory Browser helps you find the groups you’re looking for. During the scan, the FolderSecurityViewer will skip all excluded groups to keep your NTFS permissions report clean.

If you are not interested in obtaining NTFS permissions down to the leaves, FolderSecurityViewer lets you define a custom scan level to limit the analysis depth for deep directory hierarchies.


### Customizable NTFS permission reports

NTFS permission reports can be enriched with additional Active Directory user properties. For instance, you can add the email addresses of Active Directory user accounts in a report by adding the property name “mail” and a descriptive column name such as “Email” to the Active Directory Properties configuration.


### Drill-down differences in NTFS permissions

You can save your NTFS Permissions Reports to a local built-in or MS SQL Server database. Optionally, the content of the database tables can be encrypted. Saved reports can be compared against other saved reports to outline differences in permission configurations. Get insights into what permissions have been added, modified, and removed. You can even sort your report based on these criteria.

If you want to automate your NTFS permissions reporting, you can use the Command-Line Interface (CLI) of FolderSecruityViewer. This tool allows you to create all types of reports and export them either to Excel, HTML, CSV, or your database. It can be used in conjunction with Windows Task Scheduler, so your daily or weekly reports will be created automatically.


## Building FolderSecurityViewer

### Prerequisites

The application can be compiled on Windows, and depends on the following Frameworks and utilities:

* .NET Framework 4.7.2
* Optional: Visual Studio 2022


````bash
$ cd sourcecodes
$ msbuild ./FolderSecurityViewer.sln /p:Configuration=Release
````




Copyright (C) 2015 - 2024 by Carsten Schäfer, Matthias Friedrich, and Ritesh Gite
