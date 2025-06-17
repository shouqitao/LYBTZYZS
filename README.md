# LYBTZYZS

This repository contains various modules for the LYBT healthcare management system. It is organized as a multi-project .NET solution.


## Overview

The solution is composed of many projects under the `LYBT.Module.*` namespace. Each module focuses on a single area of the clinic workflow and is referenced from the `LYBTZYZS.sln` solution file.

### Key Modules

- **LYBT.Module.Billing** – handles cost settlement and invoicing.
- **LYBT.Module.Patients** – manages patient basic information.
- **LYBT.Module.Registration** – provides patient registration services.
- **LYBT.Module.Queueing** – controls queue management for visits.
- **LYBT.Module.Records** – stores and queries medical records.
- **LYBT.Module.Doctors** – maintains doctor details and related logic.
- **LYBT.Module.DiagnosisTreatment** – records diagnoses and treatments.
- **LYBT.Module.Herbs** – manages herb inventories.
- **LYBT.Module.Pharmacy** – processes dispensing at the pharmacy.
- **LYBT.Module.FormulaTemplates** – saves reusable prescription templates.
- **LYBT.Module.Prescriptions** – creates and edits patient prescriptions.
- **LYBT.Module.TreatmentRoom** – manages treatment rooms and executes assisted treatment tasks.
- **LYBT.Module.Users** – manages user accounts, roles, and authentication.
- **LYBT.Module.Settings** – stores system configuration values.
- **LYBT.Module.Sync** – synchronizes data with external systems.
- **LYBT.Module.Logs** – records application logs.

## Build and Run

Use the standard .NET CLI commands to build and start the Web API:

```bash
dotnet build
dotnet run --project LYBT.WebAPI
```


### Building the WPF Client

The `LYBT.UI.WPF` project targets `net8.0-windows`. Building on non-Windows hosts
requires the `EnableWindowsTargeting` property. A `Directory.Build.props` file is
provided to set this. Restore NuGet packages before launching the client:

```bash
dotnet restore
```

## Configuration

The API reads settings from `appsettings.json` and environment variables. An example configuration file is available at `LYBT.WebAPI/appsettings.example.json`.
Copy this file to `LYBT.WebAPI/appsettings.json` (or `appsettings.Development.json`) and update the values for your environment.

### Required environment variables

The following settings can be provided via environment variables using the `__` separator:

- `ConnectionStrings__DefaultConnection` – database connection string.
- `Jwt__Secret` – secret key used to sign JWT tokens.
- `Jwt__Issuer` – issuing authority for generated tokens.
- `Jwt__Audience` – expected audience for tokens.
- `Jwt__ExpireMinutes` – token expiration in minutes.
- `AppSettings__WorkerId` – Snowflake worker ID (optional).
- `AppSettings__DatacenterId` – Snowflake datacenter ID (optional).

These variables override values in `appsettings.json` when present.

