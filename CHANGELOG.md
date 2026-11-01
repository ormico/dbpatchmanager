# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Version numbers come from Git tags through GitVersion. Older versions are listed on the
[releases page](https://github.com/ormico/dbpatchmanager/releases).

## [Unreleased]

Changes since v2.1.2.

### Changed

- **Breaking:** Replaced Newtonsoft.Json with System.Text.Json. The Logic package no longer depends on Newtonsoft.Json.
- **Breaking:** `BuildConfigurationWriter.DetectSchemaVersion` now takes the file contents as a `string`.
- **Breaking:** Config files must be standard JSON, except that comments and trailing commas are still accepted. Single-quoted strings, unquoted property names and `NaN` are rejected.
- A `dependsOn` value that is not an array is now an error.
- `Options` booleans now read as `"true"` and `"false"` instead of `"True"` and `"False"`, and numbers keep the text used in the file.
- Upgraded to .NET 8.

### Added

- `patches.json` schema version check on read.

### Fixed

- `Write` no longer fails when `Options` is set and a `patches.local.json` file exists.
- `Read` no longer fails when `CodeFiles` or `dependsOn` is missing.
- Date-like strings in config files are no longer reformatted.
- Config load time is now linear in the number of patches.
- SQL files with no executable content are skipped with a warning, and script errors name the file and patch ([#72](https://github.com/ormico/dbpatchmanager/pull/72)).
- `--help` and `--version` now return exit code 0 ([#71](https://github.com/ormico/dbpatchmanager/pull/71)).
