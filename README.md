# LYBTZYZS

This repository contains various modules for the LYBT healthcare management system. It is organized as a multi-project .NET solution.

## Configuration

The application reads the database connection string from the standard
`DefaultConnection` setting. When running locally or in production, set the
connection string using the environment variable
`ConnectionStrings__DefaultConnection` or by providing it in configuration.

Example:

```bash
export ConnectionStrings__DefaultConnection="Server=<host>;Database=<db>;User Id=<user>;Password=<pwd>;Encrypt=True;TrustServerCertificate=True"
```
