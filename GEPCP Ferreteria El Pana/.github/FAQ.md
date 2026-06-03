# FAQ (Frequently Asked Questions)

Answers to common questions about GEPCP Ferretería El Pana.

---

## General Questions

### What is GEPCP Ferretería El Pana?
GEPCP (Gestión de Empleados y Cálculo de Planilla) is an enterprise-grade human resources and payroll management system designed specifically for small to medium-sized businesses in Costa Rica. It automates employee management, payroll calculation, vacation tracking, loans administration, and compliance reporting.

### Who should use GEPCP?
GEPCP is ideal for:
- **Small to medium businesses** (50-500 employees)
- **HR departments** managing payroll
- **Organizations** requiring compliance with Costa Rican labor law
- **Companies** needing automated payroll calculations
- **Businesses** wanting a professional, cost-effective HR solution

### What operating system does GEPCP support?
**Currently:**
- Windows 10 and newer
- Windows Server 2016 and newer

**Planned (2025):**
- Linux support (via Docker containers)
- macOS support
- Cloud-based deployment

### How much does GEPCP cost?
GEPCP is currently a **private project** for Ferretería El Pana. For inquiries about licensing or commercial use, contact the development team.

### Is GEPCP secure?
Yes. GEPCP includes:
- BCrypt password hashing
- Session-based authentication with timeout
- Role-based access control
- Comprehensive audit logging
- HTTPS/TLS support
- SQL injection and XSS prevention
- CSRF token validation

See [SECURITY.md](.github/SECURITY.md) for detailed security information.

---

## Installation & Setup

### How do I install GEPCP?
**Easiest Method:**
1. Download `Setup_GEPCP_FerreteriaElPana.exe` from GitHub Releases
2. Run as Administrator
3. Follow the installer wizard
4. Application launches automatically

See [INSTALLATION.md](.github/INSTALLATION.md) for detailed steps.

### What are the system requirements?
**Minimum:**
- Windows 10, 2 GB RAM, 500 MB storage
- 1366x768 display

**Recommended:**
- Windows 11, 4-8 GB RAM, 1+ GB SSD storage
- 1920x1080 display

### What are the default login credentials?
- **Username:** `admin.rrhh` | **Password:** `Pana2024`
- **Username:** `jefatura` | **Password:** `Pana2024`

**Important:** Change these passwords immediately after installation.

### Where is the database stored?
- **Installed:** `%LOCALAPPDATA%\GEPCP_FerreteriaElPana\Database\ferreteria.db`
- **Example:** `C:\Users\JuanPerez\AppData\Local\GEPCP_FerreteriaElPana\Database\ferreteria.db`

### Can I use GEPCP on a network?
Currently, GEPCP is designed for **single-computer** use. For network deployment (multiple users, remote access), see the [INSTALLATION.md](.github/INSTALLATION.md) advanced configuration section or contact the development team for future cloud options (2025).

### Can I move the database to another computer?
Yes:
1. Backup database file
2. Uninstall GEPCP on destination computer
3. Install GEPCP
4. Replace database file with backup
5. Restart application

See [INSTALLATION.md](.github/INSTALLATION.md) for backup/restore procedures.

### Do I need internet connection to use GEPCP?
**Core functionality:** No - works completely offline
**Email features:** Yes - requires SMTP configuration and internet

### What if the application won't start?
Common causes and solutions:
1. **Port 5002 in use** - Close conflicting application
2. **Database locked** - Wait 30 seconds and restart
3. **Missing .NET Runtime** - Install [.NET 8 Runtime](https://dotnet.microsoft.com/download)
4. **Corrupted database** - Restore from backup or reinstall

See [INSTALLATION.md](.github/INSTALLATION.md) troubleshooting section.

---

## Features & Functionality

### How do I add a new employee?
1. Login as `admin.rrhh` or `jefatura`
2. Navigate to **Employees** → **Add New**
3. Fill in employee information:
   - Personal data (name, ID, contact)
   - Employment details (position, salary, contract type)
   - Payment method (cash or bank transfer)
4. Click **Save**

Database automatically creates employee record.

### Can I import employees from Excel?
**Current Version:** No - manual entry or bulk import (planned for v2.0)

**Workaround:** Export from your current system as CSV, then manually enter into GEPCP.

### How is payroll calculated?
**Process:**
1. **Gross Pay** = Salary + Overtime + Bonuses
2. **Deductions** = CCSS (10.67%) + Income Tax + Union Fees (3%)
3. **Net Pay** = Gross Pay - Deductions

All percentages are configurable in `appsettings.json`.

### Does vacation affect the current payroll?
**No.** In Costa Rica, vacation is always paid separately. Vacation dates don't reduce current period payroll. Vacation accruals are tracked separately and paid out per labor law.

### What is the maximum number of employees supported?
- **Practical limit:** 5,000 employees with SQLite
- **Performance:** Payroll calculation takes ~2-5 seconds for 500 employees
- **Future:** Unlimited with SQL Server migration (v2.0)

### Can I recalculate payroll after approval?
**Yes.** Managers can reopen closed payroll periods:
1. Navigate to **Payroll** → **Closed Periods**
2. Click **Reopen Period**
3. Make corrections
4. Recalculate and reapprove

System maintains audit trail of all changes.

### How do loan payments work?
**Process:**
1. Create loan with principal, rate, term
2. System calculates monthly installment
3. Installment automatically deducted from payroll
4. Loan status tracked until completion
5. Employee prevented from deactivation until loan settled

### Can I generate reports?
**Available Reports:**
- Payroll receipts (PDF)
- Aguinaldo statements (PDF)
- Payroll export (Excel)
- Employee list
- Department summaries
- Audit logs

**Custom Reports:** Not yet available (planned for v2.0)

### Can employees access their payroll slips?
**Current Version:** No direct employee portal

**Workaround:** Export payroll slip as PDF and email to employee

**Future:** Employee portal planned for v2.0

---

## Email & Notifications

### How do I set up email notifications?
1. Navigate to **Settings** → **Email Configuration**
2. Enter Gmail credentials:
   - Email: `your-email@gmail.com`
   - Password: [App-specific password, not account password]
3. Click **Test Connection**
4. Click **Save**

See [INSTALLATION.md](.github/INSTALLATION.md) for detailed email setup.

### Why aren't emails sending?
Common issues:
- **Incorrect app password** - Use Gmail [App Password](https://myaccount.google.com/apppasswords), not account password
- **2FA not enabled** - Gmail requires two-factor authentication
- **Port blocked** - Firewall blocking port 587
- **Outdated configuration** - Update SMTP settings

**Test SMTP:**
```powershell
# Test Gmail connection (PowerShell)
$smtp = New-Object Net.Mail.SmtpClient("smtp.gmail.com", 587)
$smtp.EnableSsl = $true
$smtp.Credentials = New-Object System.Net.NetworkCredential("your-email@gmail.com", "your-app-password")
$smtp.Send("from@gmail.com", "to@example.com", "Test", "Test")
```

### Can I use a different email provider?
Yes. Update `appsettings.json`:
```json
{
  "Smtp": {
	"Host": "smtp.outlook.com",
	"Port": 587,
	"Username": "your-email@outlook.com",
	"Password": "your-password",
	"EnableSsl": true
  }
}
```

### How do I send payroll slips by email?
1. Calculate payroll period
2. Click **Payroll** → **Send Receipts by Email**
3. Select employees
4. Click **Send**

System generates PDFs and emails to employee addresses.

---

## Data Management

### How do I backup my data?
**Manual Backup:**
```powershell
# Backup database
Copy-Item "$env:LOCALAPPDATA\GEPCP_FerreteriaElPana\Database\ferreteria.db" `
		  -Destination "D:\Backups\ferreteria.db.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
```

**Automated Backup:** Create Windows Scheduled Task (see [INSTALLATION.md](.github/INSTALLATION.md))

### How often should I backup?
**Recommended:** Daily
- Small database (typically 1-50 MB)
- Quick backup/restore
- Minimal risk of data loss

**Critical Operations:** Before payroll calculations or large imports

### How do I restore from backup?
1. Close application
2. Navigate to `%LOCALAPPDATA%\GEPCP_FerreteriaElPana\Database\`
3. Delete corrupted `ferreteria.db`
4. Copy backup file as `ferreteria.db`
5. Restart application

### Can I export employee data?
**Current Export Options:**
- Payroll to Excel
- Audit logs to CSV/PDF
- PDF receipt generation

**Full Employee Export:** Planned for v2.0

### How long is data retained?
- **Employee records:** Per labor law (7 years)
- **Payroll history:** 7 years (tax compliance)
- **Audit logs:** 7 years (Costa Rican law)
- **Backups:** 30-day rolling retention (configurable)

### Can I delete employee records?
**Recommended:** No - instead mark as "Inactive"

This preserves payroll history and audit trail for compliance.

**Hard Delete:** Available in admin tools (use with caution)

### Can I export data to my accounting software?
**Excel Export:** Available for manual import

**Direct API Integration:** Planned for v2.0

**Workaround:** Export to Excel, then import to accounting software

---

## Security & Access Control

### How do I add a new user?
1. Login as `admin.rrhh`
2. Navigate to **Users** → **Add New User**
3. Enter username and password
4. Assign role (RRHH or Jefatura)
5. Click **Save**

### What's the difference between RRHH and Jefatura roles?

| Feature | RRHH | Jefatura |
|---------|------|----------|
| Add/Edit Employees | ✅ | ❌ |
| Calculate Payroll | ✅ | ❌ |
| Approve Payroll | ❌ | ✅ |
| View Reports | ✅ | ✅ |
| Manage Users | ✅ | ❌ |
| View Audit Logs | ✅ | ❌ |

### Can I create custom roles?
**Current Version:** No - only RRHH and Jefatura

**Future:** Custom role builder planned for v2.0

### What happens when a password expires?
**Current Version:** No password expiration

**Future:** Password expiration policy planned for v2.0

### How do I recover a lost password?
**Process:**
1. Login as another RRHH admin
2. Navigate to **Users**
3. Find user and click **Reset Password**
4. User receives temporary password
5. User changes password on next login

**Admin Recovery:** Contact system administrator

### What is audited in the system?
Everything:
- Login/logout events
- Employee data changes (before/after values)
- Payroll calculations and approvals
- Period reopenings and corrections
- User account modifications
- Data exports and reports
- Failed login attempts

View audit logs: **Settings** → **Audit Logs**

### Is data encrypted?
**Current:**
- Passwords: BCrypt hashing
- Email (SMTP): TLS encryption
- Database: Local file (OS-level file permissions)

**Future:** Column-level encryption planned for v2.0

---

## Performance & Optimization

### How long does payroll calculation take?
- **50 employees:** ~1-2 seconds
- **100 employees:** ~2-3 seconds
- **500 employees:** ~3-5 seconds

Depends on hardware and data complexity.

### What makes payroll slow?
Common causes:
- **Network drive** - Use local SSD storage
- **Antivirus scanning** - Exclude database folder
- **Low RAM** - Close other applications
- **Complex overtime/deductions** - Review data for anomalies

### Can I run multiple instances?
**No.** Application enforces single instance (Mutex). If you need multiple users:
- Use Remote Desktop
- Use VPN for remote access
- Plan multi-user cloud deployment (v2.0)

### Does GEPCP slow down my computer?
- **Memory:** 150-300 MB typical
- **CPU:** Minimal when idle
- **Disk:** Database grows ~5-10 MB per 100 employees per year

Very lightweight footprint.

### How do I optimize performance?
1. **Use SSD** - Faster database access
2. **Exclude database folder from antivirus** scanning
3. **Regular backups** - Prevents large file restoration
4. **Archive old payroll** - Reduce active database size (future feature)
5. **Use latest .NET 8 runtime** - Performance improvements

---

## Development & Customization

### Can I modify the source code?
**Yes.** Clone from GitHub and modify for your needs.

See [CONTRIBUTING.md](.github/CONTRIBUTING.md) for development setup.

### Can I create custom reports?
**Current Version:** Limited to built-in reports

**Workaround:** Export to Excel, create custom pivot tables

**Future:** Custom report builder planned for v2.0

### How do I set up a development environment?
1. Clone repository: `git clone https://github.com/HilarioSolera/GEPCP-Ferreteria-El-Pana.git`
2. Install [.NET 8 SDK](https://dotnet.microsoft.com/download)
3. Open in Visual Studio 2022
4. Run: `dotnet build`
5. Execute: `dotnet run`

See [CONTRIBUTING.md](.github/CONTRIBUTING.md) for details.

### How do I compile changes?
```powershell
cd "GEPCP Ferreteria El Pana"
dotnet build -c Release
```

### Can I contribute to the project?
Yes! See [CONTRIBUTING.md](.github/CONTRIBUTING.md) for:
- Development guidelines
- Code style standards
- Git workflow
- Pull request process

### How are bugs reported?
Create an issue on [GitHub Issues](https://github.com/HilarioSolera/GEPCP-Ferreteria-El-Pana/issues) with:
- Description
- Steps to reproduce
- Expected vs actual behavior
- Screenshots (if applicable)
- System information

### How are security issues reported?
**Do NOT post security issues publicly.**

Email security details to [security contact] instead.

See [SECURITY.md](.github/SECURITY.md) for detailed vulnerability reporting procedures.

---

## Support & Troubleshooting

### Where can I get help?
- **Documentation:** README.md, ARCHITECTURE.md, FEATURES.md
- **Installation Issues:** INSTALLATION.md
- **Security Questions:** SECURITY.md
- **Development:** CONTRIBUTING.md
- **GitHub Issues:** [Report problems](https://github.com/HilarioSolera/GEPCP-Ferreteria-El-Pana/issues)
- **Email:** [Contact information]

### How do I report a bug?
1. Verify bug is reproducible
2. Check existing GitHub Issues
3. Create new issue with:
   - Clear title
   - Detailed description
   - Reproduction steps
   - Expected vs actual behavior
   - System information

### How do I request a feature?
1. Check CHANGELOG.md roadmap section
2. Search existing issues and discussions
3. Create feature request with:
   - Clear description
   - Use case/motivation
   - Proposed solution
   - Any alternative approaches

### How quickly will my issue be resolved?
- **Critical bugs:** 24 hours
- **High priority:** 7 days
- **Normal priority:** 30 days
- **Low priority:** 90 days (or next release)

### What should I do if the database is corrupted?
1. Close application
2. Backup corrupted database (for analysis)
3. Delete `ferreteria.db`
4. Restart application (recreates fresh database)
5. Restore data from backup

### How do I uninstall GEPCP?
1. **Settings** → **Programs** → **Programs and Features**
2. Find "GEPCP Ferreteria El Pana"
3. Click "Uninstall"
4. Confirm removal
5. (Optional) Delete data: `%LOCALAPPDATA%\GEPCP_FerreteriaElPana\`

---

## Compliance & Legal

### Is GEPCP compliant with Costa Rican labor law?
Yes. GEPCP includes:
- ✅ Correct CCSS calculation (10.67%)
- ✅ Income tax deduction support
- ✅ Vacation tracking per labor code
- ✅ 7-year audit trail retention
- ✅ Payroll receipt generation
- ✅ Aguinaldo (Christmas bonus) calculations

### Do I need an accountant to use GEPCP?
No - but recommended for:
- Initial setup and validation
- Tax compliance review
- Financial reporting
- Payroll verification

### Is my data safe with GEPCP?
Yes. Security includes:
- BCrypt password protection
- Session encryption
- Audit trail of all changes
- Regular backups
- Local data storage
- Role-based access control

See [SECURITY.md](.github/SECURITY.md) for complete security information.

### What happens to my data if development stops?
- All code is open source (will remain available)
- Your data stays on your computer
- Can export to Excel/CSV for migration
- You retain full control of database

---

## Getting Started

### I just installed GEPCP. What should I do first?

1. **Change default passwords**
   - Login as `admin.rrhh`
   - Users → Edit → Change password

2. **Add your employees**
   - Employees → Add New
   - Enter employee information
   - Save

3. **Configure email** (optional)
   - Settings → Email Configuration
   - Enter Gmail or SMTP credentials
   - Test connection

4. **Create first payroll period**
   - Payroll → New Period
   - Enter start and end dates
   - Calculate payroll
   - Approve payroll

5. **Generate payroll receipts**
   - Payroll → Generate Receipts (PDF)
   - Download or email to employees

### What's the quickest way to generate payroll?
1. **Employees:** Already entered
2. **Payroll:** Click "Calculate" button
3. **Review:** Verify calculations
4. **Approve:** Manager approves
5. **Export:** PDF or Excel

Total time: ~5-10 minutes for 50 employees.

### How do I troubleshoot common issues?
1. Check [INSTALLATION.md](.github/INSTALLATION.md) troubleshooting section
2. Search [GitHub Issues](https://github.com/HilarioSolera/GEPCP-Ferreteria-El-Pana/issues)
3. Review application logs
4. Contact support with detailed error message

---

## More Questions?

Can't find your answer? 
- **Check Documentation:** README.md, ARCHITECTURE.md, FEATURES.md
- **GitHub Issues:** [Search existing issues](https://github.com/HilarioSolera/GEPCP-Ferreteria-El-Pana/issues)
- **GitHub Discussions:** [Start a discussion](https://github.com/HilarioSolera/GEPCP-Ferreteria-El-Pana/discussions)
- **Contact Team:** [Email information]

---

**Last Updated:** January 2025  
**FAQ Version:** 1.0  
**Repository:** [GitHub](https://github.com/HilarioSolera/GEPCP-Ferreteria-El-Pana)
