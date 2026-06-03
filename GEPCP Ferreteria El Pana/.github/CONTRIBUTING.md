# Contributing

Thank you for your interest in contributing to GEPCP Ferretería El Pana! This guide will help you understand how to contribute to the project effectively.

## Getting Started

### Prerequisites
- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download)
- **Visual Studio 2022** or **Visual Studio Code** with C# support
- **Git** - [Download](https://git-scm.com/)
- **SQL Server** (for production) or **SQLite** (for development)

### Setup Development Environment

1. **Clone the Repository**
   ```powershell
   git clone https://github.com/HilarioSolera/GEPCP-Ferreteria-El-Pana.git
   cd "GEPCP Ferreteria El Pana"
   ```

2. **Install Dependencies**
   ```powershell
   dotnet restore
   ```

3. **Build Project**
   ```powershell
   dotnet build -c Debug
   ```

4. **Apply Database Migrations**
   ```powershell
   dotnet ef database update
   ```

5. **Run Application**
   ```powershell
   dotnet run
   ```
   The application will open automatically at `http://localhost:5002`

### Default Test Credentials
- **Username:** `admin.rrhh` | **Password:** `Pana2024`
- **Username:** `jefatura` | **Password:** `Pana2024`

---

## Development Workflow

### 1. Create a Feature Branch
```powershell
git checkout -b feature/your-feature-name
```

### 2. Make Your Changes
- Write clean, readable code
- Follow the existing code style and conventions
- Add meaningful comments for complex logic
- Include unit tests for new functionality

### 3. Commit Your Changes
```powershell
git commit -m "feat: add new payroll feature"
```

**Commit Message Convention:**
- `feat:` - New feature
- `fix:` - Bug fix
- `docs:` - Documentation changes
- `style:` - Code style changes (formatting, semicolons, etc.)
- `refactor:` - Code restructuring without behavior change
- `perf:` - Performance improvements
- `test:` - Adding or updating tests
- `chore:` - Build/dependencies/tooling changes

### 4. Push to Remote
```powershell
git push origin feature/your-feature-name
```

### 5. Create a Pull Request
- Provide a clear description of changes
- Reference any related issues (#123)
- Ensure all tests pass
- Request code review from maintainers

---

## Code Style Guidelines

### C# Naming Conventions
```csharp
// Public classes - PascalCase
public class EmpleadoViewModel { }

// Public methods - PascalCase
public decimal CalcularSalarioNeto() { }

// Private methods - camelCase
private void ValidarDatos() { }

// Constants - SCREAMING_SNAKE_CASE
private const decimal PORCENTAJE_CCSS = 10.67m;

// Properties - PascalCase
public string NombreEmpleado { get; set; }

// Local variables - camelCase
int diasVacacion = 15;
```

### Formatting Standards
- **Indentation:** 4 spaces (never tabs)
- **Line Length:** 120 characters max
- **Braces:** Always on new line (Allman style)
- **Spaces:** Around operators (x + y, not x+y)

### Example Code Structure
```csharp
using System;
using Microsoft.EntityFrameworkCore;
using GEPCP_Ferreteria_El_Pana.Data;

namespace GEPCP_Ferreteria_El_Pana.Services
{
	public class PayrollService
	{
		private readonly ApplicationDbContext _context;

		public PayrollService(ApplicationDbContext context)
		{
			_context = context;
		}

		public async Task<PayrollResult> CalculateMonthlyPayroll(int periodId)
		{
			var empleados = await _context.Empleados
				.Where(e => e.Activo)
				.ToListAsync();

			var results = new List<PayrollResult>();
			foreach (var empleado in empleados)
			{
				var result = CalculateEmployeePayroll(empleado);
				results.Add(result);
			}

			return AggregateResults(results);
		}

		private PayrollResult CalculateEmployeePayroll(Empleado empleado)
		{
			decimal grossPay = empleado.Salario;
			decimal ccssDeduction = grossPay * 0.1067m;
			decimal netPay = grossPay - ccssDeduction;

			return new PayrollResult
			{
				EmpleadoId = empleado.EmpleadoId,
				GrossPay = grossPay,
				Deductions = ccssDeduction,
				NetPay = netPay
			};
		}
	}
}
```

---

## Architecture & Design Patterns

### MVC Pattern
```
Controllers/
  └─> EmpleadosController.cs
	  ├─> Index() - List employees
	  ├─> Create() - New employee form
	  ├─> Edit() - Edit employee form
	  └─> Delete() - Remove employee

Services/
  └─> EmployeeService.cs
	  ├─> Create(employee)
	  ├─> Update(employee)
	  ├─> Delete(employeeId)
	  └─> GetById(employeeId)

Models/
  └─> Empleado.cs (Domain Model)
	  └─> EmpleadoViewModel.cs (View Model)

Views/
  └─> Empleados/
	  ├─> Index.cshtml
	  ├─> Create.cshtml
	  └─> Edit.cshtml
```

### Dependency Injection
Always use dependency injection for services:

```csharp
// ❌ DON'T - Tightly coupled
public class PlanillaController : Controller
{
	private EmailService emailService = new EmailService();
}

// ✅ DO - Dependency Injection
public class PlanillaController : Controller
{
	private readonly EmailService _emailService;

	public PlanillaController(EmailService emailService)
	{
		_emailService = emailService;
	}
}
```

### LINQ Query Optimization
```csharp
// ❌ DON'T - N+1 problem
var empleados = _context.Empleados.ToList();
foreach (var emp in empleados)
{
	var prestamos = _context.Prestamos.Where(p => p.EmpleadoId == emp.EmpleadoId).ToList();
}

// ✅ DO - Use Include
var empleados = _context.Empleados
	.Include(e => e.Prestamos)
	.ToList();
```

### Error Handling
```csharp
try
{
	var resultado = await _service.ProcessPayroll(periodId);
	return Ok(resultado);
}
catch (InvalidOperationException ex)
{
	_logger.LogError(ex, "Payroll processing failed for period {PeriodId}", periodId);
	return BadRequest(new { error = ex.Message });
}
catch (Exception ex)
{
	_logger.LogError(ex, "Unexpected error during payroll processing");
	return StatusCode(500, new { error = "An error occurred. Please contact support." });
}
```

---

## Testing

### Unit Testing Structure
Create tests in parallel folder structure:

```
GEPCP Ferreteria El Pana/
  └─> Services/
	  └─> PayrollService.cs

GEPCP Ferreteria El Pana.Tests/
  └─> Services/
	  └─> PayrollServiceTests.cs
```

### Writing Unit Tests
```csharp
[TestClass]
public class PayrollServiceTests
{
	private PayrollService _service;
	private Mock<ApplicationDbContext> _contextMock;

	[TestInitialize]
	public void Setup()
	{
		_contextMock = new Mock<ApplicationDbContext>();
		_service = new PayrollService(_contextMock.Object);
	}

	[TestMethod]
	public async Task CalculatePayroll_WithValidEmployee_ReturnsCorrectAmount()
	{
		// Arrange
		var empleado = new Empleado
		{
			EmpleadoId = 1,
			NombreEmpleado = "Juan Pérez",
			Salario = 500000m
		};

		// Act
		var result = await _service.CalculateMonthlyPayroll(empleado);

		// Assert
		Assert.AreEqual(446650m, result.NetPay); // 500000 - 53350 (CCSS)
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentNullException))]
	public async Task CalculatePayroll_WithNullEmployee_ThrowsException()
	{
		// Act & Assert
		await _service.CalculateMonthlyPayroll(null);
	}
}
```

### Running Tests
```powershell
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter ClassName=PayrollServiceTests

# Run with coverage report
dotnet test /p:CollectCoverage=true
```

---

## Git Workflow

### Branching Strategy
```
master/
  ├─> feature/employee-management
  ├─> feature/payroll-calculation
  ├─> bugfix/email-integration
  └─> hotfix/security-patch
```

### Creating a Pull Request

1. **Push your feature branch**
   ```powershell
   git push origin feature/your-feature
   ```

2. **Open PR on GitHub**
   - Base branch: `master`
   - Compare branch: `feature/your-feature`
   - Title: Clear, descriptive title
   - Description: What changes, why, and how to test

3. **PR Template**
   ```markdown
   ## Description
   Brief description of changes.

   ## Related Issue
   Fixes #123

   ## Changes Made
   - Change 1
   - Change 2

   ## Testing
   How to test these changes:
   1. Step 1
   2. Step 2

   ## Checklist
   - [ ] Code follows style guidelines
   - [ ] Tests added/updated
   - [ ] Documentation updated
   - [ ] No breaking changes
   ```

### Code Review Process
1. At least one maintainer review required
2. All conversations resolved
3. All checks passing (build, tests)
4. Squash commits before merge (optional)

---

## Database Changes

### Adding a New Column
1. **Update Domain Model**
   ```csharp
   public class Empleado
   {
	   public int EmpleadoId { get; set; }
	   // ... existing properties
	   public string NumeroCuenta { get; set; }  // New property
   }
   ```

2. **Create Migration**
   ```powershell
   dotnet ef migrations add AddNumeroCuentaToEmpleado
   ```

3. **Review Generated Migration**
   ```csharp
   public partial class AddNumeroCuentaToEmpleado : Migration
   {
	   protected override void Up(MigrationBuilder migrationBuilder)
	   {
		   migrationBuilder.AddColumn<string>(
			   name: "NumeroCuenta",
			   table: "Empleados",
			   type: "TEXT",
			   nullable: true);
	   }

	   protected override void Down(MigrationBuilder migrationBuilder)
	   {
		   migrationBuilder.DropColumn(
			   name: "NumeroCuenta",
			   table: "Empleados");
	   }
   }
   ```

4. **Apply Migration**
   ```powershell
   dotnet ef database update
   ```

5. **Commit Changes**
   ```powershell
   git add Migrations/ Models/Empleado.cs
   git commit -m "feat: add account number field to employees"
   ```

---

## Security Best Practices

### Password Handling
```csharp
// ✅ DO - Use BCrypt
var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

// ❌ DON'T - Plain text or weak hashing
var hash = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(password));
```

### SQL Injection Prevention
```csharp
// ✅ DO - Parameterized queries (Entity Framework)
var user = _context.Usuarios
	.Where(u => u.NombreUsuario == username)
	.FirstOrDefault();

// ❌ DON'T - String concatenation
var query = $"SELECT * FROM Usuarios WHERE NombreUsuario = '{username}'";
var user = _context.Usuarios.FromSqlRaw(query).FirstOrDefault();
```

### XSS Prevention
```csharp
// ✅ DO - Razor HTML encoding (automatic)
<p>@Model.NombreUsuario</p>  // Automatically encoded

// ❌ DON'T - Raw HTML
<p>@Html.Raw(Model.NombreUsuario)</p>  // Dangerous
```

### CSRF Token
```html
<!-- ✅ DO - Include CSRF token in forms -->
<form method="post" action="/empleados/create">
	@Html.AntiForgeryToken()
	<!-- form fields -->
	<button type="submit">Save</button>
</form>
```

---

## Documentation

### Code Comments
- Write comments for **why**, not what
- Keep comments up-to-date with code
- Use XML documentation for public APIs

```csharp
/// <summary>
/// Calculates the monthly net salary for an employee after deductions.
/// </summary>
/// <param name="empleadoId">The unique employee identifier</param>
/// <returns>The calculated net salary amount in colones</returns>
/// <exception cref="ArgumentException">Thrown if empleadoId is invalid</exception>
public decimal CalculateMonthlySalary(int empleadoId)
{
	// Implementation details
}
```

### README Sections
When updating README.md:
- Keep examples simple and runnable
- Update feature list with new capabilities
- Document breaking changes clearly
- Add troubleshooting section for common issues

---

## Performance Considerations

### Database Query Optimization
```csharp
// ❌ Inefficient - 1 + N queries
var empleados = _context.Empleados.ToList();
foreach (var emp in empleados)
{
	var total = _context.Prestamos.Where(p => p.EmpleadoId == emp.EmpleadoId).Sum(p => p.Monto);
}

// ✅ Efficient - Single query
var empleados = _context.Empleados
	.Select(e => new
	{
		Empleado = e,
		PrestamoTotal = e.Prestamos.Sum(p => p.Monto)
	})
	.ToList();
```

### Caching Static Data
```csharp
// Cache department list in memory
var departments = await _context.Departamentos.ToListAsync();
_memoryCache.Set("departments", departments, TimeSpan.FromHours(24));
```

---

## Issue Reporting

### Bug Report Template
```markdown
## Description
Clear description of the bug.

## Steps to Reproduce
1. Step 1
2. Step 2
3. Step 3

## Expected Behavior
What should happen.

## Actual Behavior
What actually happens.

## Screenshots
If applicable, add screenshots.

## Environment
- OS: Windows 10
- .NET Version: 8.0
- Visual Studio: 2022
```

### Feature Request Template
```markdown
## Description
Clear description of requested feature.

## Motivation
Why this feature is needed.

## Proposed Solution
How the feature should work.

## Alternatives
Other approaches considered.

## Additional Context
Any other relevant information.
```

---

## Release Process

### Version Numbering
Follow [Semantic Versioning](https://semver.org/):
- **MAJOR.MINOR.PATCH** (e.g., 1.2.3)
- **MAJOR** - Breaking changes
- **MINOR** - New features (backward compatible)
- **PATCH** - Bug fixes (backward compatible)

### Creating a Release
1. **Update Version**
   - Update `<Version>` in `.csproj`
   - Update `CHANGELOG.md`

2. **Tag Release**
   ```powershell
   git tag -a v1.2.3 -m "Release version 1.2.3"
   git push origin v1.2.3
   ```

3. **Create GitHub Release**
   - Go to GitHub Releases
   - Create release from tag
   - Add release notes and changelog

---

## Support

### Getting Help
- **Issues** - Report bugs and request features on GitHub Issues
- **Discussions** - Ask questions in GitHub Discussions
- **Documentation** - Check ARCHITECTURE.md and FEATURES.md

### Code of Conduct
- Be respectful and inclusive
- Provide constructive feedback
- No harassment or discrimination
- Follow project guidelines

---

## License

This project is licensed under the **Private License**. Contributions are welcome but the code remains the property of Ferretería El Pana.

---

**Thank you for contributing to GEPCP Ferretería El Pana!** 🎉

Last Updated: 2025
