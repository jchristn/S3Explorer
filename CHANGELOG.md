# Changelog

All notable changes to this project are documented in this file.

## v1.0.1 (2026-10-03)

### Dependencies

- Avalonia, Avalonia.Desktop, Avalonia.Fonts.Inter, Avalonia.Themes.Fluent: 12.1.1 -> 12.1.3
- Avalonia.Controls.DataGrid: 12.1.1 -> 12.1.2
- AWSSDK.S3: 4.0.102.1 -> 4.0.104.1
- Test dependencies:
  - Touchstone.Core, Touchstone.Cli, Touchstone.XunitAdapter, Touchstone.NunitAdapter: 0.1.12 -> 0.2.0
  - Microsoft.NET.Test.Sdk: 18.9.0 -> 18.10.1
  - NUnit: 4.6.1 -> 5.0.0
  - NUnit3TestAdapter: 6.2.0 -> 6.3.0

### Other

- Application version is now declared in `S3Explorer.csproj`; installer, Chocolatey package, and `build.ps1` default bumped to 1.0.1

## v1.0.0

- Migrated to Avalonia 12
- Added Touchstone-based test infrastructure (shared suites run via CLI, xUnit, and NUnit)
- Default `ForcePathStyle` to false for AWS
- Initial release: cross-platform S3 / S3-compatible object storage browser
