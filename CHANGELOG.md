# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## [0.1.0] - 2026-03-07

### Added
- Static analysis of EF Core migration files using Roslyn
- 12 analysis rules covering data loss, missing indexes, irreversible migrations, raw SQL, rename detection, column sizing, ordering, and primary key alterations (MC001-MC012)
- Table and JSON output formats
- `--severity` filter to control minimum reported severity
- `--last N` flag to analyze only the most recent migrations
- `--no-reversibility` flag to skip Down() analysis
- `--ci` mode with non-zero exit code on error-level findings
- Auto-discovery of Migrations folder from project root
