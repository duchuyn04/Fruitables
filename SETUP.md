# 🚀 Setup Guide - Fruitables E-commerce

## 📋 Prerequisites

- .NET 8.0 SDK
- SQL Server (LocalDB or Full)
- Visual Studio 2022 or VS Code
- Git

---

## 🔧 Initial Setup

### 1. Clone Repository

```bash
git clone https://github.com/duchuy19012004/Fruitables.git
cd Fruitables
```

### 2. Configure Database Connection

Copy the example settings file:

```bash
cp appsettings.example.json appsettings.json
```

Edit `appsettings.json` and update the connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=FruitablesDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

Replace `YOUR_SERVER_NAME` with:
- `(localdb)\\mssqllocaldb` for LocalDB
- `.` or `localhost` for local SQL Server
- Your server name for remote SQL Server

### 3. Restore Dependencies

```bash
dotnet restore
```

### 4. Apply Database Migrations

```bash
dotnet ef database update
```

This will create the database and all tables including RBAC system.

### 5. Run the Application

```bash
dotnet run
```

Or press F5 in Visual Studio.

The application will be available at:
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`

---

## 🔐 RBAC System Setup

After first run, you need to migrate users to the RBAC system:

### Option 1: Via Web UI (Recommended)

1. Login with an admin account
2. Navigate to: `/Admin/Diagnostics/Migration`
3. Click "Run Migration"
4. Logout and login again

### Option 2: Via Diagnostics Page

1. Navigate to: `/Admin/Diagnostics`
2. Review system status
3. Follow the instructions to run migration

---

## 👤 Default Admin Account

After running migrations, you should have default admin users. Check your seed data or create one manually in the database.

---

## 📁 Project Structure

```
Fruitables/
├── Areas/Admin/          # Admin panel
│   ├── Controllers/      # Admin controllers
│   └── Views/           # Admin views
├── Controllers/         # Public controllers
├── Models/             # Data models
├── Services/           # Business logic
├── Repositories/       # Data access
├── ViewModels/         # View models
├── Views/              # Public views
├── wwwroot/            # Static files
└── Data/               # Database context
```

---

## 🔑 Key Features

- **RBAC System**: Role-Based Access Control with fine-grained permissions
- **User Management**: Lock/unlock accounts, manage roles
- **Order Management**: Complete order lifecycle with status tracking
- **Product Management**: Categories, products, variants
- **Revenue Statistics**: Detailed analytics and reports
- **Address Management**: Vietnam address system with provinces/districts/wards
- **Shipping Configuration**: Flexible shipping fee calculation
- **Review System**: Product reviews with moderation

---

## 🛠️ Development

### Build

```bash
dotnet build
```

### Run Tests

```bash
dotnet test
```

### Create Migration

```bash
dotnet ef migrations add MigrationName
```

### Update Database

```bash
dotnet ef database update
```

---

## 📝 Configuration Files

- `appsettings.json` - Main configuration (NOT in Git)
- `appsettings.example.json` - Template for configuration
- `appsettings.Development.json` - Development overrides (NOT in Git)

**Important:** Never commit `appsettings.json` or `appsettings.Development.json` to Git as they contain sensitive information.

---

## 🚨 Troubleshooting

### Database Connection Issues

1. Check SQL Server is running
2. Verify connection string in `appsettings.json`
3. Ensure database user has proper permissions

### Migration Issues

1. Delete the database and run migrations again
2. Check migration files for errors
3. Use `/Admin/Diagnostics` page to check system status

### RBAC Permission Issues

1. Navigate to `/Admin/Diagnostics/Migration`
2. Run RBAC migration
3. Logout and login again
4. Check user has proper roles assigned

---

## 📞 Support

For issues or questions:
1. Check the diagnostics page: `/Admin/Diagnostics`
2. Review error logs
3. Contact the development team

---

## 🔒 Security Notes

- Always use HTTPS in production
- Keep `appsettings.json` secure and never commit it
- Regularly update dependencies
- Use strong passwords for admin accounts
- Enable two-factor authentication (if implemented)

---

## 📄 License

[Your License Here]
