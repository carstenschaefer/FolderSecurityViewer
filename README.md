![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/carstenschaefer/FolderSecurityViewer/dotnet.yml)
![GitHub License](https://img.shields.io/github/license/carstenschaefer/FolderSecurityViewer)
![GitHub Release](https://img.shields.io/github/v/release/carstenschaefer/FolderSecurityViewer?include_prereleases&display_name=tag)
![Static Badge](https://img.shields.io/badge/Platform-Windows-7119FF)


This README is just a quick-start document. For more detailed information, see the [official docs](https://g-tac.gitbook.io/foldersecurityviewer).

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

* .NET Framework 8.0 SDK
* Optional: Visual Studio 2022
* MSBuild version 17.9.8 or above

### Platform compatibility

Ensure your target platform is supported by .NET 8; check the supported OS versions in the  [.NET 8 Release Notes](https://github.com/dotnet/core/blob/main/release-notes/8.0/supported-os.md#net-8---supported-os-versions). If you need a build compatible with Windows 7 or 8, please refer to version [v2.7.0-beta.1](https://github.com/carstenschaefer/FolderSecurityViewer/releases/tag/v2.7.0-beta.1), which depends on .NET Framework 4.7 and Visual Studio.


### Build

````bash
$ cd sourcecodes
$ dotnet build ./FolderSecurityViewer.sln --configuration Release
````

### Run tests

````bash
$ dotnet clean && dotnet build
$ dotnet test --framework net8.0-windows
````


Copyright (C) 2015 - 2024 by Carsten Schäfer, Matthias Friedrich, and Ritesh Gite
