# TipBuddyApi

This is the back-end portion of the TipBuddy web application. It is built with C# and .NET 9, using Entity Framework Core for data access.

## Features
- User authentication and registration
- Shift management (CRUD operations)
- Custom JSON date handling
- Repository pattern for data access
- AutoMapper for DTO mapping
- JWT authentication
- Swagger/OpenAPI documentation

## Projects
- **TipBuddyApi**: Main API project
- **TipBuddyApi.Tests**: Unit tests for API logic

## Getting Started
1. **Restore NuGet packages**
   - Use the Package Manager Console:
     ```powershell
     Install-Package <PackageName>
     ```
2. **Build the solution**
   - Run:
     ```powershell
     dotnet build
     ```
3. **Run the API**
   - From the API project directory:
     ```powershell
     dotnet run
     ```
4. **Run tests**
   - From the test project directory:
     ```powershell
     dotnet test
     ```

## NuGet Packages
For installing NuGet packages, use the Package Manager Console command:
```
Install-Package <PackageName>
```
This is the default method for package installation in this project.

## Code Coverage
To view code coverage, install the `coverlet.collector` package and run:
```
dotnet test TipBuddyApi.Tests --collect:"XPlat Code Coverage"
```

### Generate Coverage Report
To generate a coverage report, run the following PowerShell script from your solution directory:

```powershell
./generate-coverage-report.ps1
```

This will execute the script and produce a coverage report as defined in the file.

## Conventions
- **Dates**: All dates are stored in UTC format. Convert to local time on the client side.

## Contributing
- Please follow the guidance in this README for package management and development practices.

## License
This project is licensed under the MIT License.
