This is the back-end portion of the TipBuddy web application. This is a C# .NET Core project using Code-First Entity Framework to model the database.

## Conventions

### Dates
All dates stored in the database are in UTC format. Ensure that any date/time fields in the database are handled and stored in UTC, and any conversion to local times should happen on the client side.
