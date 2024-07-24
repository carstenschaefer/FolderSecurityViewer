# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [v2.8.0-beta.1] - 2024-07-25

### Changed

- Converts all project files to the new SDK format
  - Updates `AssemblyInfo.cs` files (removes attributes that get generated during the build)
- Upgrades all referenced NuGet packages
- Fixes all build errors related to API changes
  - Fixes impersonation helpers in Interop code
  - Removes exception constructors for binary serialization (no longer needed)
  - Removes `app.config` files (cleans static binding redirects)
- Removes functionality that downloads update installers (due to dead infrastructure)


## [v2.7.0-beta.1] - 2024-03-24

- Initial release