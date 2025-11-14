# Contributing Guidelines

We appreciate you taking the time to contribute to FolderSecurityViewer. This project is provided as source code only and maintained on a best-effort basis.

## Opening Issues

Before opening an issue, please check whether it meets the following expectations:

* **Bug reports must be reproducible on the latest (pre-)release or current `main` branch.**
  Issues affecting older versions will not be reviewed.

* **Security-related reports must not contain technical details.**
  Follow the instructions in `SECURITY.md` instead.

* **Feature requests should be realistic and within the project’s scope.**
  This tool focuses on permission analysis — requests that expand it into broader administration tooling will not be considered.

When opening an issue, include:

* Clear steps to reproduce (if applicable)
* Relevant environment details (OS, .NET version)
* The commit or release you tested

## Pull Requests

Pull requests are welcome, but they are expected to follow a few simple rules:

* **Target the `main` branch.**
  Contributions for older releases are not accepted.

* **Keep PRs small and focused.**
  One change per PR — avoid mixing refactors, formatting, or unrelated fixes.

* **Match the existing coding style.**
  Follow standard .NET conventions and the patterns already used in the codebase.

* **Avoid unnecessary complexity.**
  Maintainability and clarity take priority over adding new features.

* **Dependency updates:**
  Only submit updates that are necessary for security fixes or to resolve known issues. Large or speculative dependency bumps will not be accepted.

If a PR changes functional behavior, include a brief explanation of *why* the change is necessary.

## Licensing

By submitting a contribution, you agree that it will be released under the project’s existing license (AGPLv3).
