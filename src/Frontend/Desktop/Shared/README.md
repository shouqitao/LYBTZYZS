# Shared Services

This directory contains shared service interfaces that can be accessed across different workbenches.

## Interfaces

- **ISharedPatientService** - Patient management shared functionality
- **ISharedHerbService** - Herb data access shared functionality
- **ISharedFormulaService** - Formula management shared functionality
- **ISharedPrescriptionService** - Prescription shared functionality

These interfaces enable functionality sharing between:
- SystemWorkbench (Administrator)
- ConsultationWorkbench (Doctor)
- ReceptionWorkbench (Reception - Future)