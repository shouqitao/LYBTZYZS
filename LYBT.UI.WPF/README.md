# LYBT.UI.WPF

## Overview

This project is the desktop client for the LYBT system. It is a Windows Presentation Foundation (WPF) application built with Prism for modular MVVM patterns and Refit for communicating with the accompanying WebAPI.

## Prerequisites

- .NET 8 SDK
- Windows environment (WPF)

## Build and Run

1. Restore and build the project:
   ```bash
   dotnet build
   ```
2. Run the application:
   ```bash
   dotnet run --project LYBT.UI.WPF
   ```

## Main Features

- User login with role-based navigation
- Management screens for modules such as patients, billing and pharmacy
- UI theme based on MaterialDesign
- HTTP communication via Refit with the LYBT WebAPI

