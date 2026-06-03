# GEPCP Ferretería El Pana - Human Resources & Payroll Management System

A comprehensive enterprise-grade payroll and human resources management system built with .NET 8 for Ferretería El Pana. This project demonstrates full-stack web development with role-based access control, automated payroll calculations, PDF report generation, and secure data management.

## 🎯 Overview

**GEPCP** (Gestión de Empleados y Cálculo de Planilla) is a production-ready web application designed to streamline HR and payroll operations. It provides comprehensive employee management, automated biweekly/monthly payroll calculations, vacation tracking, loans administration, and compliance reporting with secure authentication and audit trails.

## ✨ Key Features

### Employee Management
- Complete CRUD operations for employee records
- Personal and occupational information tracking
- Automatic salary and deduction calculations
- Multiple payment methods support (cash, bank transfer)
- Emergency contact management
- Department and position assignment

### Payroll Processing
- Automated biweekly/monthly payroll calculations
- Configurable deduction calculations (CCSS, income tax, union fees)
- Multi-step approval workflow with role-based authorization
- PDF receipt generation for employee records
- Excel export for accounting integration
- Clear segregation between earnings and deductions

### Additional Modules
- **Vacation Management** - Paid vacation tracking and accrual calculations
- **Christmas Bonus (Aguinaldo)** - Automatic calculation and distribution
- **Overtime Management** - Normal and double-time hour tracking
- **Incapacity Management** - Sick leave and incapacity record tracking
- **Loan Administration** - Employee loan tracking and repayment scheduling
- **User Management** - Role-based access control with multi-user support

## 🔒 Security Features
- Session-based authentication with secure password management
- BCrypt password hashing
- Role-based authorization filters (HR, Management)
- Comprehensive audit logging of all operations
- Secure configuration management for sensitive data

## 💻 Technical Stack

### Backend
- **.NET 8.0** - Modern cross-platform framework
- **ASP.NET Core MVC** - Server-side rendering with Razor Pages
- **Entity Framework Core** - Object-relational mapping
- **SQLite** - Lightweight, serverless database

### Data & Reporting
- **QuestPDF** - Professional PDF generation
- **ClosedXML** - Excel export functionality
- **MailKit/MimeKit** - Email integration for payroll distribution

### Frontend
- **Bootstrap 5** - Responsive UI framework
- **Chart.js** - Interactive data visualization
- **JavaScript** - Client-side form handling and validation

### DevOps & Deployment
- **Inno Setup** - Windows installer creation
- **Entity Framework Migrations** - Database versioning
- **Automated deployment** - Application auto-start configuration

## 🏗️ Project Architecture

```
GEPCP Ferreteria El Pana/
├── Controllers/          # MVC controllers (7+ action handlers)
├── Data/                # Entity Framework DbContext
├── Filters/             # Custom authorization filters
├── Helpers/             # Utility functions
├── Migrations/          # Database schema versioning
├── Models/              # Domain and view models
├── Services/            # Business logic (Auth, PDF, Email, Audit)
├── Views/               # Razor templates
│   ├── Account/         # Authentication pages
│   ├── Empleados/       # Employee management
│   ├── Planilla/        # Payroll processing
│   ├── Aguinaldo/       # Christmas bonus
│   ├── Vacaciones/      # Vacation management
│   ├── HorasExtras/     # Overtime tracking
│   ├── Incapacidades/   # Incapacity management
│   ├── Prestamos/       # Loan administration
│   ├── Usuarios/        # User management
│   └── Shared/          # Layout and partials
├── wwwroot/             # Static assets (CSS, JS, images)
├── Program.cs           # Application entry point
└── appsettings.json     # Configuration settings
```

## 🚀 Quick Start

### Prerequisites
- Windows 10+ or Linux with .NET 8 runtime
- Visual Studio 2022 or VS Code (for development)
- .NET SDK 8.0+

### Installation & Running

#### Option 1: Windows Installer (Recommended)
1. Locate `Setup_GEPCP_FerreteriaElPana.exe` in the `Instalador/` folder
2. Run as Administrator
3. Follow the installation wizard
4. Application launches automatically on first run and at system startup

#### Option 2: Direct Execution
```powershell
cd "GEPCP Ferreteria El Pana"
dotnet build -c Release
dotnet run --configuration Release
```

The application will start at `http://localhost:5002`

### Default Credentials

| Role | Username | Password | Permissions |
|------|----------|----------|-------------|
| HR | admin.rrhh | Pana2024 | Employee management, payroll calculation, deductions |
| Management | jefatura | Pana2024 | Approval workflow, reporting, consultation |

## ⚙️ Configuration

### Application Settings (`appsettings.json`)

**Database:**
```json
{
  "ConnectionStrings": {
	"DefaultConnection": "Data Source=%LOCALAPPDATA%\\GEPCP Ferreteria El Pana\\Database\\ferreteria.db"
  }
}
```

**Business Rules:**
```json
{
  "ReglasNegocio": {
	"PorcentajeCCSS": 10.67,
	"PorcentajeAsociacion": 3.0,
	"HorasExtrasDobleMultiplicador": 2.0
  }
}
```

**Email Integration:**
```json
{
  "Smtp": {
	"Host": "smtp.gmail.com",
	"Port": 587,
	"Username": "your-email@gmail.com",
	"Password": "your-app-password",
	"EnableSsl": true
  }
}
```

## 📊 Reporting & Exports

- **Payroll Receipts** - PDF generation for employee verification
- **Aguinaldo Reports** - Christmas bonus statements
- **Excel Exports** - Batch payroll data for accounting systems
- **Digital Signatures** - PDF receipt authentication

## 🔄 Workflow

1. **Employee Registration** → Add employee with personal, occupational, and payment information
2. **Payroll Period Setup** → Define calculation dates and parameters
3. **Data Entry** → Record overtime, incapacities, and manual adjustments
4. **Calculation** → System automatically computes gross pay, deductions, and net salary
5. **Review & Approval** → Management reviews and approves payroll
6. **Distribution** → Generate PDF receipts or export to Excel
7. **Email Delivery** → Send payroll receipts to employees (optional)

## 📈 Development Workflow

### Build Project
```powershell
cd "GEPCP Ferreteria El Pana"
dotnet build -c Release
```

### Run Tests
```powershell
dotnet test
```

### Create Database Migrations
```powershell
dotnet ef migrations add DescriptiveName
dotnet ef database update
```

### Generate Windows Installer
1. Ensure Inno Setup 6 is installed
2. Run: `& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" Instalador\installer.iss`
3. Installer output: `Instalador\Setup_GEPCP_FerreteriaElPana.exe`

## 💾 Database

**Engine:** SQLite  
**Location:** `%LOCALAPPDATA%\GEPCP Ferreteria El Pana\Database\ferreteria.db`  
**Migrations:** Automatic on application startup  
**Seeding:** Default users created automatically  

## 🔐 Production Considerations

- Configure real SMTP credentials for email notifications
- Use strong, unique passwords for production user accounts
- Enable Windows Firewall rules if accessing remotely
- Implement regular database backups
- Monitor audit logs for compliance and security
- Consider TLS/SSL configuration for multi-user environments

## 📦 NuGet Dependencies

```
BCrypt.Net-Next (4.1.0) - Password hashing
ClosedXML (0.105.0) - Excel generation
EntityFrameworkCore.Sqlite (8.0.25) - Database ORM
QuestPDF (2026.2.3) - PDF generation
MailKit (4.x) - Email functionality
```

## 🎓 Learning Outcomes & Skills Demonstrated

This project showcases expertise in:
- **.NET 8 & ASP.NET Core** - Modern web framework fundamentals
- **Clean Architecture** - Separation of concerns with controllers, services, and models
- **Database Design** - Relational schema with Entity Framework migrations
- **Security** - Authentication, authorization, password hashing, and audit trails
- **PDF/Excel Generation** - Document generation and export functionality
- **Responsive Web Design** - Bootstrap-based UI with accessibility considerations
- **Business Logic** - Complex payroll calculations with tax and deduction handling
- **Deployment** - Windows installer creation and application lifecycle management

## 📄 License

Private project for Ferretería El Pana.

## 📞 Contact & Support

For technical inquiries or support: [Contact information]

---

**Version:** 1.0  
**Status:** ✅ Production Ready  
**Last Updated:** 2025  
**Developer:** Solera
