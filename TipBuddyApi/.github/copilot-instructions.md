## Date/Time Best Practices

- Always use `DateTimeOffset` for new date/time properties in C# code, unless there is a specific reason to use `DateTime`.
- Prefer `DateTimeOffset.UtcNow` for default values.
- Avoid using `DateTime` for properties that represent a specific moment in time.