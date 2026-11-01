# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Version numbers come from Git tags through GitVersion. Older versions are listed on the
[releases page](https://github.com/ormico/dbpatchmanager/releases).

## [Unreleased]

Changes since v2.1.2.

### Changed

- **Breaking:** Replaced Newtonsoft.Json with System.Text.Json. The Logic package no longer depends on Newtonsoft.Json, and `Patch` no longer carries its attributes.
- **Breaking:** `BuildConfigurationWriter.DetectSchemaVersion` now takes the file contents as a `string`.
- **Breaking:** `patches.json` and `patches.local.json` must now be standard JSON, apart from the exceptions below. Single-quoted strings, unquoted property names and `NaN` are rejected, with an error that names the file. Comments and trailing commas are still accepted.
- A `dependsOn` value that is not an array is now an error instead of being ignored.
- Boolean values in `Options` are now read as `"true"` and `"false"` instead of `"True"` and `"False"`. Numbers keep the text used in the file, so `1.50` is no longer read as `1.5`.
- Upgraded to .NET 8.

### Added

- `patches.json` schema version check on read.

### Fixed

- `Write` no longer fails when `Options` is set and a `patches.local.json` file exists.
- `Read` no longer fails when `CodeFiles` or a patch's `dependsOn` is missing.
- Date-like strings in configuration files are no longer reformatted.
- Loading configuration is now linear in the number of patches.
- SQL files with no executable content are skipped with a warning for all plugins, and script errors now name the file and patch ([#72](https://github.com/ormico/dbpatchmanager/pull/72)).
- `--help`, `--version` and the help and version verbs now return exit code 0 ([#71](https://github.com/ormico/dbpatchmanager/pull/71)).
