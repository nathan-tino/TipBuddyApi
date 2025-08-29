## Date/Time Best Practices

- Always use `DateTimeOffset` for new date/time properties in C# code, unless there is a specific reason to use `DateTime`.
- Prefer `DateTimeOffset.UtcNow` for default values.
- Avoid using `DateTime` for properties that represent a specific moment in time.

## SQL Mapping for Date/Time

- When configuring EF Core models, set the SQL column type to `datetimeoffset(0)` for all `DateTimeOffset` properties, unless a different precision is required.
- Example:
- modelBuilder.Entity<MyEntity>().Property(e => e.MyDate).HasColumnType("datetimeoffset(0)");
