# Deployment Architecture

# Current (on-premises / IIS)

```text
Angular
    │
    ▼
IIS
    │
    ▼
.NET API
    │
    ▼
SQL Server
```

- Angular built to static assets and served via IIS.
- .NET API hosted behind IIS.
- SQL Server as the database.

---

# Future (Azure)

```text
Angular
    │
    ▼
Azure Static Web App
    │
    ▼
App Service
    │
    ▼
Azure SQL
```

- Angular hosted on Azure Static Web Apps.
- .NET API hosted on Azure App Service.
- Azure SQL as the managed database.
- Secrets/keys sourced from Azure Key Vault.

The application code stays the same across targets; only hosting and configuration differ.
