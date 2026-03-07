# dotnet-migration-check

A .NET global tool that statically analyzes EF Core migration files and flags risky database operations before they reach production.

## Why?

A single bad migration can drop tables, lose data, or cause hours of downtime. These issues are easy to miss in code review because migration files are often large and auto-generated. **dotnet-migration-check** catches destructive operations, missing rollback logic, and subtle mistakes through static analysis — no database connection needed. Run it locally or in CI to stop dangerous migrations before they ship.

## Installation

```bash
dotnet tool install -g dotnet-migration-check
```

## Quick Start

Run the tool in any project directory that contains EF Core migrations:

```bash
dotnet migration-check
```

Example output:

```
┌──────────┬───────┬─────────────────────┬────────────────────────────────────────────┬──────┬──────────────────────────────────┐
│ Severity │ Rule  │ Migration           │ Message                                    │ Line │ Remediation                      │
├──────────┼───────┼─────────────────────┼────────────────────────────────────────────┼──────┼──────────────────────────────────┤
│ Critical │ MC001 │ RemoveOldTables      │ Table 'LegacyUsers' is being dropped       │   12 │ Back up data before dropping      │
│ Error    │ MC002 │ CleanupFields        │ Column 'Email' dropped from 'Users'        │    8 │ Verify column data is migrated   │
│ Warning  │ MC006 │ CleanupFields        │ Down() method is empty                     │      │ Implement Down() for rollback    │
│ Warning  │ MC008 │ RenameColumns        │ Possible rename: 'Name' → 'FullName'       │   15 │ Use RenameColumn instead         │
│ Info     │ MC007 │ SeedData             │ Raw SQL detected                           │   22 │ Review SQL for safety            │
└──────────┴───────┴─────────────────────┴────────────────────────────────────────────┴──────┴──────────────────────────────────┘
```

## Usage

```
dotnet migration-check [OPTIONS] [PATH]

Arguments:
  PATH                  Project directory or Migrations folder (default: current dir)

Options:
  --format <FORMAT>     table (default), json
  --severity <LEVEL>    info, warning (default), error, critical
  --last <N>            Only analyze last N migrations
  --no-reversibility    Skip Down() analysis
  --ci                  No color, exit 1 on error+ findings
  --version / --help
```

## Rules Reference

| ID | Name | Severity | Description |
|-|-|-|-|
| MC001 | Table dropped | Critical | DropTable() detected |
| MC002 | Column dropped | Error | DropColumn() detected |
| MC003 | Column type narrowed | Error | AlterColumn() with potentially narrower type |
| MC004 | Non-null without default | Warning | Non-nullable column added without default value |
| MC005 | FK without index | Warning | AddForeignKey() without matching CreateIndex() |
| MC006 | Empty Down() | Warning | Down() has no migrationBuilder calls |
| MC007 | Raw SQL | Info | migrationBuilder.Sql() detected |
| MC008 | Possible rename | Warning | Drop+Add on same table suggests rename |
| MC009 | Oversized column | Info | String column without maxLength |
| MC010 | Index dropped | Warning | DropIndex() detected |
| MC011 | Ordering issue | Warning | Migration timestamps not sequential |
| MC012 | PK alteration | Critical | Primary key change detected |

## CI Integration

```yaml
- name: Check migrations
  run: dotnet migration-check --ci --severity error
```

The `--ci` flag disables color output and returns exit code 1 if any findings at or above the specified severity are detected.

## License

MIT
