# Security Policy

## Security Statement

GEPCP Ferretería El Pana takes security seriously. This document outlines our security practices, vulnerability reporting procedures, and recommendations for users to maintain a secure environment.

---

## Supported Versions

| Version | Status | Support Until |
|---------|--------|----------------|
| 1.0.x | Current | 2027 |
| 0.9.x | Deprecated | 2024 |

Security updates are provided for the current version and the previous stable release.

---

## Security Features

### Authentication & Authorization

**Password Security**
- Passwords hashed using **BCrypt** with salting
- Adaptive cost factor (12 rounds) increases with CPU advancement
- Never stored in plaintext
- Secure password comparison prevents timing attacks

**Session Management**
- Session-based authentication (not cookie-based)
- 30-minute inactivity timeout
- `HttpOnly` flag prevents JavaScript access
- `SameSite=Lax` protects against CSRF attacks
- Secure flag (HTTPS-only in production)

**Role-Based Access Control**
- Two roles: `RRHH` (HR) and `Jefatura` (Management)
- Controller-level authorization filters
- Attribute-based access control
- Role validation on every request

### Data Protection

**Input Validation**
- Server-side validation for all user inputs
- Client-side validation for user experience
- Whitelist approach (accept known-good, reject everything else)
- Regular expression pattern matching for sensitive fields

**SQL Injection Prevention**
- Entity Framework Core with parameterized queries
- LINQ to SQL automatic parameterization
- No raw SQL concatenation
- Prepared statements for all database operations

**Cross-Site Scripting (XSS) Prevention**
- Razor HTML encoding by default
- `@Model.Property` automatically encoded
- `@Html.Raw()` only used for trusted content
- Content Security Policy headers (future enhancement)

**Cross-Site Request Forgery (CSRF) Prevention**
- AntiForgeryToken on all form submissions
- Token validation on POST/PUT/DELETE operations
- SameSite cookie attribute
- Referer header validation

### Network Security

**HTTPS/TLS**
- Production environment enforces HTTPS
- HSTS (Strict-Transport-Security) header included
- TLS 1.2+ only
- Strong cipher suites configured

**Environment-Based Security**
```
Development:
  - HTTPS disabled for ease of testing
  - Detailed error messages
  - Entity Framework SQL logging enabled
  - Debug information included

Production:
  - HTTPS enforced
  - Generic error pages (no sensitive data)
  - SQL logging disabled
  - Stack traces hidden
```

### Audit & Logging

**Comprehensive Audit Trail**
- All operations logged with:
  - User responsible
  - Action type (Create, Update, Delete, etc.)
  - Timestamp (millisecond precision)
  - Changed data (before/after values)
  - IP address (if network-based)

**Searchable History**
- Filter by user, date range, action type
- Full-text search capabilities
- Export audit logs to CSV/PDF
- Retention policy (configurable)

**Sensitive Operation Logging**
- Failed login attempts
- Privilege escalation attempts
- Password changes
- User account modifications
- Payroll period reopenings
- Data exports

### Configuration Security

**Sensitive Data Management**
- No hardcoded credentials in source code
- Configuration stored in `appsettings.json` (not in repository)
- Environment variables for production secrets
- AWS Secrets Manager/Azure Key Vault (future)

**Email Credentials**
- Gmail App Passwords recommended (not account passwords)
- 2FA required for Gmail accounts
- Credentials never logged or exposed
- SMTP over TLS with proper certificate validation

---

## Vulnerability Reporting

### Reporting a Vulnerability

We appreciate security researchers and users who report vulnerabilities responsibly.

**Do NOT:**
- Publicly disclose the vulnerability
- Post on GitHub issues
- Share vulnerability details on social media
- Exploit vulnerability for personal gain

**Instead:**
- Email: [security@example.com] (or maintainer)
- Include:
  - Vulnerability description
  - Affected version(s)
  - Steps to reproduce
  - Potential impact
  - Suggested fix (if available)
  - Your contact information

### Response Timeline

| Timeframe | Action |
|-----------|--------|
| Within 24 hours | Acknowledge receipt |
| Within 7 days | Initial assessment |
| Within 30 days | Patch release or mitigation plan |
| Within 60 days | Public disclosure (if applicable) |

### Security Advisories

After vulnerability resolution:
1. Security advisory published
2. Patch release distributed
3. CVE identifier requested (if applicable)
4. Update instructions provided
5. Acknowledgment of reporter (if desired)

---

## Security Best Practices for Users

### Installation & Setup

1. **Use Official Installer**
   - Download from [GitHub Releases](https://github.com/HilarioSolera/GEPCP-Ferreteria-El-Pana/releases)
   - Verify file signatures (future)
   - Do not use untrusted sources

2. **Change Default Passwords**
   - Immediately after installation
   - Use strong, unique passwords (12+ characters)
   - Store securely in password manager
   - Never share credentials

3. **Keep System Updated**
   - Windows security updates
   - .NET runtime updates
   - Application security patches

4. **Configure Email Securely**
   - Use Gmail App Passwords (not account password)
   - Enable 2FA on email account
   - Test email configuration thoroughly

### Daily Operations

1. **Access Control**
   - Create individual user accounts
   - Use only necessary role permissions
   - Regularly review active users
   - Remove access for terminated employees

2. **Data Protection**
   - Regular database backups (daily recommended)
   - Store backups securely (encrypted)
   - Test restore procedures
   - Document backup schedule

3. **Monitoring**
   - Review audit logs weekly
   - Check for suspicious login attempts
   - Monitor unusual data access
   - Alert on failed operations

4. **Network Security**
   - Use firewall rules (port 5002)
   - VPN for remote access (if applicable)
   - Disable remote access if not needed
   - Use strong network passwords

### Compliance & Governance

1. **Data Governance**
   - Comply with local labor laws
   - Maintain audit trail (Costa Rican requirements)
   - Document data retention policies
   - Regular compliance audits

2. **Access Management**
   - Principle of least privilege
   - Segregation of duties (calculation vs. approval)
   - Regular access reviews
   - Documented role definitions

3. **Incident Response**
   - Document incidents
   - Preserve evidence
   - Contact security team
   - Report to relevant authorities (if required)

---

## Secure Development Practices

### Code Security

**OWASP Top 10 Mitigation**
| Vulnerability | Mitigation | Status |
|---|---|---|
| Injection | Parameterized queries, EF Core | ✅ |
| Broken Authentication | BCrypt, session timeout, 2FA (future) | ✅ |
| Sensitive Data Exposure | HTTPS, encryption at rest (future) | ✅ |
| XML External Entities | No XML parsing | ✅ |
| Broken Access Control | RBAC, authorization filters | ✅ |
| Security Misconfiguration | Environment-based config | ✅ |
| XSS | HTML encoding, CSP headers (future) | ✅ |
| Insecure Deserialization | Type validation, input checking | ✅ |
| Using Components with Known Vulnerabilities | Regular NuGet updates | ✅ |
| Insufficient Logging | Comprehensive audit trail | ✅ |

**Dependency Management**
```powershell
# Check for vulnerable packages
dotnet list package --vulnerable

# Update packages
dotnet package update

# Review security advisories
# Regularly check GitHub security alerts
```

**Code Review**
- All changes reviewed before merge
- Security-focused code reviews
- Automated security scanning
- Static analysis tools (future)

### Secure Configuration

**appsettings.json Template**
```json
{
  "Smtp": {
	"Username": "[Set via environment variable]",
	"Password": "[Set via environment variable]"
  }
}
```

**Environment Variables (Production)**
```powershell
[Environment]::SetEnvironmentVariable("SMTP_USERNAME", "email@gmail.com", "Machine")
[Environment]::SetEnvironmentVariable("SMTP_PASSWORD", "[app-password]", "Machine")
```

### Secure Deployment

**Installer Security**
- Digital signing of executable (future)
- Verification of file integrity
- Secure update mechanism
- No automatic admin escalation

**Application Hardening**
- Single-instance enforcement (Mutex)
- No debug symbols in production
- Remove unnecessary endpoints
- Rate limiting (future)

---

## Encryption

### Current Encryption

**In Transit**
- TLS 1.2+ for HTTPS connections
- SMTP over TLS (port 587)
- Secure cookies with HttpOnly flag

**At Rest**
- BCrypt for password hashing
- Database stored locally (OS-level file permissions)
- No column-level encryption (future enhancement)

### Future Encryption Improvements

1. **Transparent Data Encryption (TDE)**
   - Database-level encryption at rest
   - Operating system provides key management
   - Minimal performance impact

2. **Column-Level Encryption**
   - Sensitive fields encrypted individually
   - Salary, SSN, account numbers
   - Queryable while encrypted (Always Encrypted, future)

3. **End-to-End Encryption**
   - Email body encryption
   - PDF document encryption
   - Secure file transmission

---

## Incident Response

### Incident Classification

| Level | Severity | Examples | Response |
|-------|----------|----------|----------|
| Critical | High impact, wide exposure | Data breach, authentication bypass | Immediate action within hours |
| High | Significant impact | Privilege escalation, audit bypass | Response within 24 hours |
| Medium | Moderate impact | Information disclosure | Response within 7 days |
| Low | Minimal impact | Missing validation | Address in regular update |

### Response Steps

1. **Detection & Triage**
   - Identify issue severity
   - Assess scope and impact
   - Gather evidence

2. **Containment**
   - Disable affected functionality (if necessary)
   - Notify users of potential exposure
   - Implement temporary mitigations

3. **Eradication**
   - Develop and test fix
   - Review code for similar issues
   - Prepare security patch

4. **Recovery**
   - Deploy fix to production
   - Monitor for issues
   - Verify remediation

5. **Post-Incident**
   - Conduct post-mortem analysis
   - Document lessons learned
   - Update security procedures
   - Share transparency report (if appropriate)

---

## Security Testing

### Regular Security Activities

**Penetration Testing**
- Annual external penetration test
- Focus on authentication and authorization
- Database security assessment
- Network segment testing

**Vulnerability Scanning**
```powershell
# SonarQube analysis (future)
dotnet-sonarscanner begin /k:"gepcp"
dotnet build
dotnet-sonarscanner end

# Dependabot alerts
# GitHub automatically monitors NuGet packages
```

**Security Code Review**
- Focus on critical sections
- Authentication/authorization paths
- Data access and modification
- Error handling and logging

---

## Privacy Policy

### Data Collection
GEPCP Ferretería El Pana collects:
- Employee personal information (as required for payroll)
- Usage statistics for system improvement (future)
- Audit logs for compliance and security
- System diagnostics (error reports)

### Data Retention
- Employee records: Retained per labor law requirements
- Audit logs: 7 years (Costa Rican tax compliance)
- Backups: 30 days rolling retention
- Error logs: 90 days

### Data Security
- Data encrypted in transit (HTTPS/TLS)
- Data encrypted at rest (future)
- Access restricted to authorized personnel
- Regular security audits

### User Rights
- Right to access personal data
- Right to correct inaccurate data
- Right to data portability
- Right to be forgotten (with limitations)

---

## Third-Party Security

### Dependency Security

**NuGet Packages Used**
- BCrypt.Net-Next - Password hashing
- EntityFrameworkCore - Database access
- QuestPDF - PDF generation
- ClosedXML - Excel generation
- MailKit - Email sending

**Vulnerability Management**
```powershell
# Check for vulnerabilities
dotnet list package --vulnerable

# Update to safe version
dotnet package update BCrypt.Net-Next

# Review advisory
# Visit: https://www.nuget.org/packages/BCrypt.Net-Next
```

### Monitoring
- GitHub Dependabot monitors for vulnerable packages
- Weekly dependency update reviews
- Security advisory tracking
- Automated security patching (future)

---

## Compliance

### Regulatory Compliance

**Costa Rican Labor Law**
- Employee record retention (7 years)
- Payroll calculation accuracy
- Tax deduction compliance
- Audit trail maintenance

**Data Protection**
- GDPR compliance (if applicable)
- Personal data handling procedures
- Consent management
- Data privacy by design

### Certifications & Standards

**Target Certifications** (Future)
- ISO 27001 (Information Security Management)
- SOC 2 Type II
- COBIT compliance

---

## Security Contacts

- **Security Issues:** [security contact email]
- **GitHub Security:** [Link to security advisories]
- **Emergency:** [Emergency contact number]

---

## Security Changelog

### Version 1.0 (January 2025)
- ✅ BCrypt password hashing
- ✅ Session-based authentication
- ✅ Role-based access control
- ✅ Comprehensive audit logging
- ✅ HTTPS/TLS support
- ✅ CSRF token validation
- ✅ SQL injection prevention

### Planned Improvements
- [ ] Two-factor authentication (2FA)
- [ ] API key authentication
- [ ] Database-level encryption
- [ ] IP whitelisting
- [ ] Account lockout after failed attempts
- [ ] Session device fingerprinting
- [ ] Automated security scanning

---

**Last Updated:** January 2025  
**Next Review:** July 2025  
**Maintainer:** Development Team

For questions or concerns about security, please contact the development team or file an issue on GitHub (do not disclose security details publicly).
