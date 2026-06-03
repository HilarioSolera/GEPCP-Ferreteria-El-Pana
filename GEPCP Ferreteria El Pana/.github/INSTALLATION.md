# Installation & Deployment Guide

## System Requirements

### Minimum Requirements
- **OS:** Windows 10 / Windows Server 2016+
- **RAM:** 2 GB
- **Storage:** 500 MB free space
- **Display:** 1366x768 minimum resolution
- **Internet:** Connection required for first launch (credential verification)

### Recommended Requirements
- **OS:** Windows 11 / Windows Server 2019+
- **RAM:** 4-8 GB
- **Storage:** SSD with 1+ GB free space
- **Display:** 1920x1080 or higher
- **Processor:** 4+ cores @ 2.0+ GHz

### Network Requirements
- **SMTP Access:** Port 587 (Gmail) or configured SMTP server
- **Internal Network:** Port 5002 for application access
- **Firewall:** Allow localhost:5002 traffic

---

## Installation Methods

## Method 1: Windows Installer (Recommended)

### Prerequisites
- Windows 10 or newer
- Administrator rights
- Internet connection (optional, for email features)

### Installation Steps

1. **Download Installer**
   - Locate `Setup_GEPCP_FerreteriaElPana.exe` in the `Instalador/` folder
   - Or download from [GitHub Releases](https://github.com/HilarioSolera/GEPCP-Ferreteria-El-Pana/releases)

2. **Run Installer**
   - Right-click on `Setup_GEPCP_FerreteriaElPana.exe`
   - Select "Run as Administrator"

3. **Follow Installation Wizard**
   ```
   Welcome Screen
	  ↓
   License Agreement (Accept to continue)
	  ↓
   Installation Location (Default: C:\Program Files\GEPCP Ferreteria El Pana)
	  ↓
   Start Menu Folder
	  ↓
   Additional Tasks
	  - [✓] Create desktop shortcut
	  - [✓] Run on Windows startup
	  - [✓] Create Start Menu shortcuts
	  ↓
   Ready to Install
	  ↓
   Installation Progress
	  ↓
   Finish (Application launches automatically)
   ```

4. **Verify Installation**
   - Application window opens with login screen
   - Check "Start Menu" → "GEPCP Ferreteria El Pana"
   - Desktop shortcut created
   - Database initialized

### Post-Installation

1. **First Login**
   - Username: `admin.rrhh`
   - Password: `Pana2024`

2. **Change Default Passwords** (Recommended)
   - Navigate to Users Management
   - Update both user passwords
   - Document new credentials securely

3. **Configure Email** (Optional)
   - Go to application settings
   - Enter Gmail credentials
   - Generate [App Password](https://myaccount.google.com/apppasswords) for Gmail accounts
   - Test email delivery

4. **Customize Business Rules** (Optional)
   - Configure tax percentages
   - Set overtime multipliers
   - Define bank list and payment methods

### Uninstall

1. **Windows Control Panel**
   - Go to "Programs and Features"
   - Find "GEPCP Ferreteria El Pana"
   - Click "Uninstall"
   - Confirm removal

2. **Remove Data** (Optional)
   - Database located at: `%LOCALAPPDATA%\GEPCP_FerreteriaElPana\Database\`
   - Backup before deletion

---

## Method 2: Manual Installation (Development)

### Prerequisites
- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download)
- **Visual Studio 2022** or **VS Code**
- **Git** - [Download](https://git-scm.com/)

### Installation Steps

1. **Clone Repository**
   ```powershell
   git clone https://github.com/HilarioSolera/GEPCP-Ferreteria-El-Pana.git
   cd "GEPCP Ferreteria El Pana"
   ```

2. **Restore Dependencies**
   ```powershell
   dotnet restore
   ```

3. **Build Project**
   ```powershell
   dotnet build -c Release
   ```

4. **Apply Database Migrations**
   ```powershell
   dotnet ef database update
   ```

5. **Run Application**
   ```powershell
   dotnet run --configuration Release
   ```

   Application opens at `http://localhost:5002`

### Creating Installer from Source

1. **Install Inno Setup 6**
   - Download from [jrsoftware.org](https://jrsoftware.org/isdl.php)
   - Run installer with default settings

2. **Publish Application**
   ```powershell
   cd "GEPCP Ferreteria El Pana"
   dotnet publish -c Release -o publish
   ```

3. **Generate Installer**
   ```powershell
   cd Instalador
   & "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer.iss
   ```

   Output: `Instalador\Setup_GEPCP_FerreteriaElPana.exe`

---

## Method 3: Portable Execution

### Prerequisites
- **.NET 8 Runtime** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- No installation required

### Execution Steps

1. **Extract Application**
   - Extract published application files to desired folder
   - Or use deployment folder from build

2. **Set Environment Variables** (Optional)
   ```powershell
   # Disable automatic browser launch
   [System.Environment]::SetEnvironmentVariable("GEPCP_NO_AUTO_BROWSER", "1", "User")
   ```

3. **Run Application**
   ```powershell
   cd C:\Path\To\Application
   dotnet GEPCP_Ferreteria_El_Pana.dll
   ```

### Advantages
- No administrator rights required
- Easy backup and migration
- Multiple versions on same computer
- USB drive portable installation

---

## Database Setup

### Database Location
```
Development:
  Database/ferreteria.db (project folder)

Installed:
  %LOCALAPPDATA%\GEPCP_FerreteriaElPana\Database\ferreteria.db
  Example: C:\Users\JuanPerez\AppData\Local\GEPCP_FerreteriaElPana\Database\
```

### Database Initialization
- Automatic on first application launch
- Schema created from EF Core migrations
- Default users seeded automatically
- No manual intervention required

### Database Backup

**Manual Backup**
```powershell
# Locate database
$dbPath = "$env:LOCALAPPDATA\GEPCP_FerreteriaElPana\Database"

# Create backup
Copy-Item -Path "$dbPath\ferreteria.db" `
		  -Destination "$dbPath\ferreteria.db.backup.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
```

**Automated Backup Script**
```powershell
# Create scheduled task for daily backup
$scriptPath = "C:\Scripts\backup-gepcp.ps1"

$action = New-ScheduledTaskAction -Execute "powershell.exe" `
	-Argument "-File $scriptPath"

$trigger = New-ScheduledTaskTrigger -Daily -At 2:00 AM

Register-ScheduledTask -TaskName "GEPCP-Database-Backup" `
	-Action $action -Trigger $trigger -RunLevel Highest
```

### Database Restoration

1. **Stop Application**
   - Close application window
   - Ensure no locks on database file

2. **Restore from Backup**
   ```powershell
   # Remove current database
   Remove-Item "$env:LOCALAPPDATA\GEPCP_FerreteriaElPana\Database\ferreteria.db"

   # Restore backup
   Copy-Item -Path "$env:LOCALAPPDATA\GEPCP_FerreteriaElPana\Database\ferreteria.db.backup.20250101_020000" `
			 -Destination "$env:LOCALAPPDATA\GEPCP_FerreteriaElPana\Database\ferreteria.db"
   ```

3. **Restart Application**
   - Launch application normally
   - Verify data restoration

### Database Migration

**From SQLite to SQL Server** (Future)
```powershell
# Export from SQLite
dotnet ef database script --output migration.sql

# Connect to SQL Server
# Update appsettings.json connection string
# Apply migrations
dotnet ef database update
```

---

## Configuration

### appsettings.json

Located at application root (development) or embedded in installer.

```json
{
  "ConnectionStrings": {
	"DefaultConnection": "Data Source=%LOCALAPPDATA%\\GEPCP_FerreteriaElPana\\Database\\ferreteria.db"
  },
  "ReglasNegocio": {
	"PorcentajeCCSS": 10.67,
	"PorcentajeAsociacion": 3.0,
	"PorcentajeOtrasDeducciones": 0.0,
	"HorasExtrasDobleMultiplicador": 2.0
  },
  "Smtp": {
	"Host": "smtp.gmail.com",
	"Port": 587,
	"Username": "your-email@gmail.com",
	"Password": "your-app-password",
	"EnableSsl": true
  },
  "Logging": {
	"LogLevel": {
	  "Default": "Information"
	}
  }
}
```

### Modifying Configuration

**For Installed Version:**
- Configuration embedded during installation
- Contact administrator for changes
- May require application restart

**For Development Version:**
- Edit `appsettings.json` directly
- Changes take effect on application restart

### Environment Variables

```powershell
# Disable automatic browser launch
set GEPCP_NO_AUTO_BROWSER=1

# Set application environment
set ASPNETCORE_ENVIRONMENT=Production
```

---

## Email Configuration

### Gmail Setup (Recommended)

1. **Create Gmail Account or Use Existing**
   - Email: `gepcp@gmail.com` or similar

2. **Enable 2-Factor Authentication**
   - Google Account settings
   - Security → Two-Step Verification
   - Follow setup wizard

3. **Generate App Password**
   - Visit [Google App Passwords](https://myaccount.google.com/apppasswords)
   - Select "Mail" and "Windows Computer"
   - Copy generated password (16 characters)

4. **Configure in Application**
   - Settings → Email Configuration
   - Host: `smtp.gmail.com`
   - Port: `587`
   - Username: `your-email@gmail.com`
   - Password: `[paste app password]`
   - Enable SSL: ✓
   - Test Connection

### Alternative Email Providers

**Outlook/Microsoft 365**
```json
{
  "Smtp": {
	"Host": "smtp.office365.com",
	"Port": 587,
	"Username": "your-email@outlook.com",
	"Password": "your-password",
	"EnableSsl": true
  }
}
```

**Custom SMTP Server**
```json
{
  "Smtp": {
	"Host": "mail.company.com",
	"Port": 587,
	"Username": "user@company.com",
	"Password": "your-password",
	"EnableSsl": true
  }
}
```

---

## Network Configuration

### Port Configuration

**Default Port: 5002**

To change port (development only):
1. Edit `Program.cs`
2. Update `const int PUERTO = 5002;`
3. Rebuild and run

### Firewall Configuration

**Windows Firewall**
```powershell
# Allow inbound traffic
netsh advfirewall firewall add rule name="GEPCP-Port-5002" `
	dir=in action=allow protocol=TCP localport=5002

# Remove rule
netsh advfirewall firewall delete rule name="GEPCP-Port-5002"
```

### Remote Access (Advanced)

To access application from another computer:

1. **Update URL Configuration**
   ```csharp
   // In Program.cs
   builder.WebHost.UseUrls("http://0.0.0.0:5002");
   ```

2. **Configure Firewall** (as shown above)

3. **Access from Remote PC**
   ```
   http://[computer-ip]:5002
   ```

**Security Warning:** Remote access without HTTPS/VPN is not recommended for production.

---

## Performance Tuning

### Database Performance

**Index Creation**
```sql
-- Add indexes for frequently queried columns
CREATE INDEX idx_empleados_cedula ON Empleados(Cedula);
CREATE INDEX idx_empleados_activo ON Empleados(Activo);
CREATE INDEX idx_planillas_estado ON Planillas(Estado);
```

**Query Optimization**
- Use `Include()` to prevent N+1 queries
- Materialize queries only when necessary
- Use projections to reduce data transfer

### Application Performance

**Memory Management**
- Monitor memory usage in Task Manager
- Clear old audit logs periodically
- Archive completed payroll periods

**Application Restart**
```powershell
# Restart application service
Stop-Process -Name "GEPCP*" -Force
Start-Sleep -Seconds 2
& "C:\Program Files\GEPCP Ferreteria El Pana\GEPCP.exe"
```

---

## Troubleshooting

### Application Won't Start

**Check Port Conflict**
```powershell
# Check if port 5002 is in use
netstat -ano | findstr :5002

# Find process using port
Get-Process -Id [PID]
```

**Reset Application Data**
```powershell
# Back up database
Copy-Item "$env:LOCALAPPDATA\GEPCP_FerreteriaElPana" -Destination "$env:LOCALAPPDATA\GEPCP_FerreteriaElPana.backup"

# Remove application data
Remove-Item "$env:LOCALAPPDATA\GEPCP_FerreteriaElPana" -Recurse

# Restart application
```

### Database Errors

**"Database is locked"**
- Close application
- Wait 30 seconds
- Restart application

**"No such column"**
- Database schema mismatch after update
- Run migrations: `dotnet ef database update`
- Delete database and let application recreate it

### Email Not Sending

**Test SMTP Connection**
```powershell
$smtp = New-Object Net.Mail.SmtpClient("smtp.gmail.com", 587)
$smtp.EnableSsl = $true
$smtp.Credentials = New-Object System.Net.NetworkCredential("your-email@gmail.com", "your-app-password")

try {
	$smtp.Send("from@gmail.com", "to@example.com", "Test", "Test email")
	Write-Host "Success"
} catch {
	Write-Host "Error: $_"
}
```

**Common Issues**
- Incorrect app password (use 16-char Gmail app password, not account password)
- 2FA not enabled on Gmail account
- Firewall blocking SMTP port 587
- Outdated TLS/SSL configuration

### Performance Issues

**High Memory Usage**
- Close other applications
- Reduce number of concurrent operations
- Archive old audit logs
- Restart application

**Slow Payroll Calculation**
- Verify database indexes are created
- Check for missing employee data
- Profile queries in Entity Framework logging
- Consider breaking into smaller batches

---

## Security Hardening

### Change Default Credentials
1. Login as `admin.rrhh`
2. Navigate to User Management
3. Edit both default users
4. Change passwords to strong, unique values
5. Document securely (password manager)

### Disable Automatic Browser Launch
```powershell
[System.Environment]::SetEnvironmentVariable("GEPCP_NO_AUTO_BROWSER", "1", "User")
```

### Enable HTTPS (Development)
```csharp
// In Program.cs
if (app.Environment.IsDevelopment())
{
	app.UseHttpsRedirection();
}
```

### Regular Maintenance
- Apply security updates monthly
- Review audit logs weekly
- Update OWASP Top 10 checks
- Backup database daily

---

## Upgrade & Maintenance

### Checking for Updates
- Visit [GitHub Releases](https://github.com/HilarioSolera/GEPCP-Ferreteria-El-Pana/releases)
- Compare version numbers
- Review release notes for breaking changes

### Upgrade Process
1. **Backup Current Installation**
   ```powershell
   Copy-Item "C:\Program Files\GEPCP Ferreteria El Pana" `
			 -Destination "C:\Program Files\GEPCP Ferreteria El Pana.backup"
   ```

2. **Backup Database**
   ```powershell
   Copy-Item "$env:LOCALAPPDATA\GEPCP_FerreteriaElPana\Database\ferreteria.db" `
			 -Destination "$env:LOCALAPPDATA\GEPCP_FerreteriaElPana\Database\ferreteria.db.backup"
   ```

3. **Uninstall Current Version**
   - Programs and Features → Uninstall

4. **Install New Version**
   - Run new installer
   - Follow installation wizard

5. **Verify Data Integrity**
   - Login and check recent payroll data
   - Verify all employee records
   - Test report generation

---

## Support & Troubleshooting Resources

- **GitHub Issues:** [Report bugs and request features](https://github.com/HilarioSolera/GEPCP-Ferreteria-El-Pana/issues)
- **Documentation:** Check ARCHITECTURE.md and FEATURES.md
- **Email Support:** Contact project maintainers

---

**Last Updated:** 2025  
**Version:** 1.0
