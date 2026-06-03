# System Architecture

## Overview

GEPCP Ferretería El Pana is built on a **layered architecture** following clean code principles with clear separation of concerns. The application uses the MVC pattern with Entity Framework Core for data persistence.

```
┌─────────────────────────────────────────────────────────────┐
│                    Client Layer (Razor Views)              │
│              Bootstrap 5 UI + JavaScript Interactivity     │
└────────────────────────┬────────────────────────────────────┘
						 │
┌────────────────────────▼────────────────────────────────────┐
│              Controller Layer (MVC Controllers)             │
│         Request Routing, Authorization Filters             │
└────────────────────────┬────────────────────────────────────┘
						 │
┌────────────────────────▼────────────────────────────────────┐
│           Business Logic Layer (Services)                  │
│  ┌─────────────────────────────────────────────────────┐  │
│  │ • AuthService - Authentication & Authorization     │  │
│  │ • ComprobantePlanillaService - Payroll Receipts   │  │
│  │ • EmailService - Notification Delivery            │  │
│  │ • AuditoriaService - Audit Trail Management       │  │
│  └─────────────────────────────────────────────────────┘  │
└────────────────────────┬────────────────────────────────────┘
						 │
┌────────────────────────▼────────────────────────────────────┐
│        Data Access Layer (Entity Framework Core)            │
│              ApplicationDbContext - SQLite ORM              │
└────────────────────────┬────────────────────────────────────┘
						 │
┌────────────────────────▼────────────────────────────────────┐
│         Persistence Layer (SQLite Database)                │
│   %LOCALAPPDATA%\GEPCP_FerreteriaElPana\Database\           │
└─────────────────────────────────────────────────────────────┘
```

## Key Components

### 1. **Controllers** (`Controllers/`)
Handles HTTP requests and orchestrates business logic.

- **HomeController** - Dashboard and analytics
- **EmpleadosController** - Employee CRUD operations
- **PlanillaController** - Payroll calculations and processing
- **AguinaldoController** - Christmas bonus management
- **VacacionesController** - Vacation tracking
- **HorasExtrasController** - Overtime record management
- **IncapacidadesController** - Incapacity/sick leave tracking
- **PrestamosController** - Employee loans administration
- **UsuariosController** - User account management
- **AccountController** - Authentication flows

### 2. **Services** (`Services/`)
Encapsulates business logic and external integrations.

#### **AuthService**
```csharp
- ValidateCredentials(username, password) : bool
- CreateUserSession(userId) : void
- HashPassword(password) : string
- VerifyPassword(input, hash) : bool
```
Manages user authentication with BCrypt password hashing and session-based authorization.

#### **ComprobantePlanillaService**
```csharp
- GenerarComprobanteEmpleado(planillaId) : byte[]
- ExportarPlanillaExcel(periodoId) : byte[]
- CalcularMontos(empleado) : PlanillaDetalles
```
Generates PDF receipts and Excel exports using QuestPDF and ClosedXML.

#### **EmailService**
```csharp
- EnviarComprobanteEmpleado(empleadoId, pdf) : Task
- EnviarPlanillaGerencia(planillaId) : Task
```
Sends payroll notifications via Gmail SMTP with configurable credentials.

#### **AuditoriaService**
```csharp
- RegistrarAccion(usuario, accion, detalles) : Task
- ObtenerHistorial(filtros) : IEnumerable<AuditoriaLog>
```
Logs all system operations for compliance and security auditing.

### 3. **Models** (`Models/`)
Domain entities and view models.

#### **Domain Models**
- **Empleado** - Employee master record
- **Usuario** - System user accounts
- **Planilla** - Payroll period details
- **PlanillaDetalle** - Individual employee payroll lines
- **Prestamo** - Employee loans
- **Aguinaldo** - Christmas bonus records
- **Vacacion** - Vacation accrual and usage
- **HoraExtra** - Overtime records
- **Incapacidad** - Incapacity tracking
- **Auditoria** - Audit trail logs

#### **View Models**
- **EmpleadoViewModel** - Employee form binding
- **PlanillaViewModel** - Payroll processing form
- **DashboardViewModel** - KPI and analytics data

### 4. **Data Context** (`Data/ApplicationDbContext.cs`)
Entity Framework Core DbContext managing database operations.

```csharp
DbSet<Empleado> Empleados
DbSet<Usuario> Usuarios
DbSet<Planilla> Planillas
DbSet<PlanillaDetalle> PlanillaDetalles
DbSet<Prestamo> Prestamos
DbSet<Aguinaldo> Aguinaldos
DbSet<Vacacion> Vacaciones
DbSet<HoraExtra> HorasExtras
DbSet<Incapacidad> Incapacidades
DbSet<Auditoria> Auditorias
```

### 5. **Filters** (`Filters/`)
Custom authorization and security filters.

- **AutorizacionRolAttribute** - Role-based access control
- **AuditarAccionAttribute** - Automatic audit logging

### 6. **Views** (`Views/`)
Razor templates for server-side rendering.

Each view uses Bootstrap 5 for responsive design and includes client-side validation.

## Data Flow Example: Payroll Processing

```
1. Manager logs in
   └─> AuthService.ValidateCredentials()
	   └─> User session created

2. Navigate to Payroll Module
   └─> PlanillaController.Index()
	   └─> Query Empleados from ApplicationDbContext
		   └─> Display employee list

3. Click "Calculate Payroll"
   └─> PlanillaController.Calcular(POST)
	   └─> For each empleado:
		   ├─> Calculate gross pay (salary + bonuses + overtime)
		   ├─> Calculate deductions (CCSS, taxes, union fees)
		   ├─> Create PlanillaDetalle record
		   └─> Save to database

4. Generate PDF Receipt
   └─> ComprobantePlanillaService.GenerarComprobanteEmpleado()
	   └─> QuestPDF renders formatted receipt
		   └─> Return as byte[] for download

5. Send Email Notification
   └─> EmailService.EnviarComprobanteEmpleado()
	   └─> SMTP connection to Gmail
		   └─> Attach PDF and send to employee

6. Log Action
   └─> AuditoriaService.RegistrarAccion()
	   └─> Create Auditoria record in database
		   └─> Timestamp, user, action, details recorded
```

## Database Schema

### Empleados Table
```sql
CREATE TABLE Empleados (
	EmpleadoId INTEGER PRIMARY KEY,
	Nombre TEXT NOT NULL,
	Cedula TEXT UNIQUE NOT NULL,
	Email TEXT,
	Telefono TEXT,
	Salario DECIMAL(10,2),
	Activo BOOLEAN,
	FechaIngreso DATE,
	FechaVencimientoContrato DATE,
	TipoContrato TEXT, -- PlazoFijo, Indefinido
	Banco TEXT,
	NumeroCuenta TEXT,
	MetodoPago TEXT, -- Efectivo, Transferencia
	RelacionContactoEmergencia TEXT,
	...
);
```

### Planillas Table
```sql
CREATE TABLE Planillas (
	PlanillaId INTEGER PRIMARY KEY,
	FechaInicio DATE NOT NULL,
	FechaFin DATE NOT NULL,
	Estado TEXT, -- Pendiente, Aprobada, Pagada
	FechaAprobacion DATE,
	UsuarioAprobacionId INTEGER,
	...
);
```

### Auditorias Table
```sql
CREATE TABLE Auditorias (
	AuditoriaId INTEGER PRIMARY KEY,
	UsuarioId INTEGER,
	Accion TEXT,
	FechaHora DATETIME,
	Detalles TEXT,
	IPAddress TEXT,
	...
);
```

## Security Architecture

### Authentication
- **Session-based** authentication (not cookie-based)
- **BCrypt hashing** for password storage (salted, adaptive cost)
- **Session timeout** of 30 minutes for inactivity

### Authorization
- **Role-Based Access Control (RBAC)**
  - `RRHH` - Full access to employee and payroll management
  - `Jefatura` - Read-only access and approval workflows
- **Attribute-based filters** on sensitive controllers

### Data Protection
- **HTTPS redirection** in production
- **CSRF tokens** on all state-changing forms
- **HttpOnly cookies** for session tokens
- **Audit logging** of all operations

### Environment-Based Security
```
Development:
  - HTTPS disabled for ease of testing
  - Detailed error pages
  - Entity Framework logging enabled

Production:
  - HTTPS enforced (UseHsts)
  - Generic error pages
  - Sensitive data masked in logs
```

## Performance Considerations

### Database Optimization
- **Lazy loading disabled** - Use explicit includes to prevent N+1 queries
- **Indexing** on frequently queried columns (EmpleadoId, FechaIngreso)
- **Query materialization** with `.ToList()` only when necessary

### Caching Strategies
- **Static dropdown data** cached in memory
- **Dashboard metrics** computed on-demand or refreshed hourly
- **Department/Position lists** fetched with minimal database hits

### Scalability Notes
- Single-instance application (enforced via Mutex)
- SQLite suitable for 100-500 concurrent operations
- For larger deployments, migrate to SQL Server with connection pooling

## Deployment Architecture

### Development
```
Visual Studio 2022
  └─> dotnet run
	  └─> http://localhost:5002 (auto-launch)
```

### Production (Windows)
```
Inno Setup Installer
  └─> Installs to Program Files
	  └─> Creates Windows Service (optional)
		  └─> Auto-start on system boot
			  └─> Single-instance enforcement (Mutex)
```

### Application Lifecycle
1. **Startup**
   - Mutex check for single instance
   - Database migrations applied
   - Default users seeded (if missing)
   - Default positions initialized

2. **Runtime**
   - Session middleware active
   - Expired-contract employee auto-deactivation
   - Request logging and audit trail
   - Auto-browser launch (configurable)

3. **Shutdown**
   - DbContext disposed properly
   - Session cleanup
   - Audit finalization

## Configuration Management

### appsettings.json
```json
{
  "ConnectionStrings": {
	"DefaultConnection": "Data Source=%LOCALAPPDATA%\\GEPCP_FerreteriaElPana\\Database\\ferreteria.db"
  },
  "ReglasNegocio": {
	"PorcentajeCCSS": 10.67,
	"PorcentajeAsociacion": 3.0,
	"HorasExtrasDobleMultiplicador": 2.0
  },
  "Smtp": {
	"Host": "smtp.gmail.com",
	"Port": 587,
	"Username": "your-email@gmail.com",
	"Password": "your-app-password",
	"EnableSsl": true
  }
}
```

### Environment Variables
- `GEPCP_NO_AUTO_BROWSER=1` - Disable automatic browser launch
- `ASPNETCORE_ENVIRONMENT` - Switch between Development/Production profiles

## Technology Decisions & Rationale

| Component | Choice | Rationale |
|-----------|--------|-----------|
| Framework | .NET 8 / ASP.NET Core | Modern, performant, cross-platform; active LTS support |
| Rendering | Razor Pages/MVC | Server-side rendering, simpler mental model than SPA |
| Database | SQLite | Lightweight, serverless, sufficient for SMB payroll needs |
| ORM | Entity Framework Core | LINQ support, automatic migrations, industry standard |
| PDF Generation | QuestPDF | Modern API, excellent performance, design flexibility |
| Excel Export | ClosedXML | Intuitive API, better than NPOI for modern Office formats |
| Password Security | BCrypt | Adaptive cost, industry-standard, resistant to GPU attacks |
| Email | MailKit | Replaces deprecated SmtpClient, full OAuth2 support |
| Authentication | Session-based | Appropriate for monolithic web app, simpler than JWT for this use case |

## Future Architectural Improvements

1. **Service-Oriented Architecture (SOA)**
   - Separate microservice for payroll calculations
   - Dedicated reporting service
   - Enables independent scaling and deployment

2. **Event-Driven Architecture**
   - Event sourcing for audit trail immutability
   - Real-time notifications via SignalR
   - Asynchronous processing of exports

3. **CQRS Pattern**
   - Separate read/write models
   - Optimize read-heavy dashboard queries
   - Improve performance for reporting

4. **Cloud Migration**
   - Azure SQL Database (SQL Server)
   - Azure App Service for hosting
   - Azure Key Vault for secrets management
   - Azure Storage for document archival

5. **API Layer**
   - RESTful API for third-party integrations
   - GraphQL for flexible querying
   - Enable mobile app development

---

**Last Updated:** 2025  
**Maintainer:** Development Team
