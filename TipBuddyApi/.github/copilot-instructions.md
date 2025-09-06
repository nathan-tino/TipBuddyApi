## Date/Time Best Practices

- Always use `DateTimeOffset` for new date/time properties in C# code, unless there is a specific reason to use `DateTime`.
- Prefer `DateTimeOffset.UtcNow` for default values.
- Avoid using `DateTime` for properties that represent a specific moment in time.

## SQL Mapping for Date/Time

- When configuring EF Core models, set the SQL column type to `datetimeoffset(0)` for all `DateTimeOffset` properties, unless a different precision is required.
- Example:
- modelBuilder.Entity<MyEntity>().Property(e => e.MyDate).HasColumnType("datetimeoffset(0)");

## ID Column Best Practices

- All ID columns for entities (including User and Shift) should use `nvarchar(36)` in the database to match GUID string length.
- Update future migrations and models accordingly to ensure consistency and optimal storage.
- When creating tables, ensure all ID columns and foreign keys referencing IDs use `nvarchar(36)`.
- Example:
  - modelBuilder.Entity<MyEntity>().Property(e => e.Id).HasMaxLength(36).HasColumnType("nvarchar(36)");
  - migrationBuilder.CreateTable(... Id = table.Column<string>(type: "nvarchar(36)", ...

## Migration Best Practices

- Always use the migrationBuilder.RenameXYZ methods (e.g., RenameTable, RenameColumn) for renaming tables or columns in migrations instead of using raw SQL statements like EXEC sp_rename. This ensures type safety and better compatibility with Entity Framework Core.
