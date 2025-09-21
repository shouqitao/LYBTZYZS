# Workbench Core

This directory contains the core infrastructure for workbench routing and navigation.

## Core Components

- **IWorkbenchRouter** - Interface for role-to-workbench mapping
- **WorkbenchRouter** - Implementation of workbench routing logic
- **NavigationItem** - Model for navigation menu items
- **IWorkbenchNavigator** - Interface for workbench-specific navigation

## Workbench Mapping

- Administrator → SystemWorkbench
- Doctor → ConsultationWorkbench
- Reception (Future) → ReceptionWorkbench