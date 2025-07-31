# LYBT Traditional Chinese Medicine System - Quick Start Guide

## 📋 Available Scripts

### 🚀 Quick Launch (Recommended)

**For Development:**
- `Quick-Start-Dev.bat` - Immediately start development server
- `启动控制台.bat` - Main control console (English interface)

**For Verification:**
- `Verify-Scripts.bat` - Check if all scripts and dependencies are properly set up

### 🔧 Detailed Scripts (in scripts/ folder)

**Development:**
- `scripts\start-dev-en.bat` - Start development server
- `scripts\main-en.bat` - Main control console

**Production:**
- `scripts\publish-production.bat` - Build production version
- `scripts\deploy-all.bat` - One-click deployment with configuration wizard

**Database:**
- `scripts\database-manager.bat` - Database management tool

## 🎯 How to Use

### Step 1: Verify Setup
1. Double-click `Verify-Scripts.bat`
2. Make sure all items show "EXISTS" and .NET is "INSTALLED"

### Step 2: Choose Your Method

**Method A: Quick Development Start**
```
Double-click: Quick-Start-Dev.bat
```
This will immediately start the development server.

**Method B: Full Control Console**
```
Double-click: 启动控制台.bat
```
This opens a menu with all available options.

**Method C: Direct Script Execution**
```
Double-click any script in the scripts/ folder
```

## 🔍 Troubleshooting

### Common Issues:

1. **"不是内部或外部命令" (Command not found)**
   - This is usually a character encoding issue
   - Use the English versions: `main-en.bat`, `start-dev-en.bat`

2. **".NET not found"**
   - Install .NET 8.0 SDK from https://dotnet.microsoft.com/download

3. **"Project directory not found"**
   - Make sure you're running scripts from the project root directory
   - The directory structure should be:
     ```
     LYBTZYZS/
     ├── scripts/
     ├── src/Backend/Services/LYBT.WebAPI/
     └── Quick-Start-Dev.bat
     ```

4. **Database connection issues**
   - Make sure SQL Server is running
   - Check connection string in `appsettings.json`

## 📁 Generated Files

After using deployment scripts, you'll find:
- `publish/` - Production build
- `publish/start-production.bat` - Production server launcher
- `publish/install-guide.md` - Deployment instructions

## 🌐 Default URLs

- **Development**: http://localhost:5297
- **Production**: http://localhost:5000
- **API Documentation**: /swagger
- **Health Check**: /api/health

## 💡 Tips

1. **First time setup**: Run `Verify-Scripts.bat` to check everything is ready
2. **Quick development**: Use `Quick-Start-Dev.bat` for immediate server start
3. **Full control**: Use `启动控制台.bat` for access to all tools
4. **Production deployment**: Use the deploy wizard in the main console

## 📞 Support

If you encounter issues:
1. Check the console output for error messages
2. Verify all prerequisites are installed
3. Make sure you're in the correct directory
4. Check the log files for detailed error information