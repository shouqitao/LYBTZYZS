---
created: 2025-09-03T13:20:35Z
last_updated: 2025-09-03T13:20:35Z
version: 1.0
author: Claude Code PM System
---

# Product Context

## Product Overview

**凌隐宝堂中医诊所诊疗系统 (LYBTZYZS)** is a comprehensive Traditional Chinese Medicine (TCM) clinic management system designed specifically for small to medium-sized TCM clinics in China.

## Target Users

### Primary Users
1. **TCM Doctors (中医师)**
   - Age: 35-65 years
   - Tech proficiency: Basic to intermediate
   - Needs: Efficient patient management, prescription creation, medical record keeping
   - Pain points: Paper-based records, prescription calculations, herb inventory

2. **Clinic Administrators (诊所管理员)**
   - Role: Oversee clinic operations
   - Needs: User management, system configuration, reporting
   - Pain points: Manual scheduling, paper filing, compliance tracking

3. **Receptionists (接待员)**
   - Role: Patient registration and appointment management
   - Needs: Quick patient lookup, appointment scheduling
   - Pain points: Manual registration, appointment conflicts

### Secondary Users
1. **Patients (患者)**
   - Indirect users through printed prescriptions and reports
   - Benefit from organized medical records and professional prescriptions

## Core Functionality

### 1. Patient Management (患者管理)
- **Patient Registration**: Complete patient profile creation
- **Medical History**: Comprehensive health records
- **Visit Tracking**: Historical consultation records
- **Search & Filter**: Advanced patient search capabilities

### 2. TCM Consultation (中医诊断)
- **Four Diagnostic Methods (四诊)**:
  - Observation (望诊): Visual examination records
  - Auscultation & Olfaction (闻诊): Sound and smell observations
  - Interrogation (问诊): Patient symptom questioning
  - Palpation (切诊): Pulse and physical examination
- **Syndrome Differentiation (辨证论治)**: TCM pattern diagnosis
- **Treatment Principles (治则治法)**: Treatment strategy documentation

### 3. Prescription Management (处方管理)
- **Herb Selection**: Database of Chinese medicinal herbs
- **Dosage Calculation**: Automatic dosage recommendations
- **Formula Templates**: Classic prescription templates (验方)
- **Compatibility Checking**: Herb interaction warnings
- **Prescription Printing**: Professional formatted prescriptions

### 4. Herb Management (中药材管理)
- **Herb Database**: Comprehensive herb information
  - Chinese name (中文名)
  - Pinyin (拼音)
  - Properties (性味归经)
  - Functions (功效主治)
  - Dosage ranges (用量范围)
- **Price Management**: Unit pricing and updates
- **Usage Guidelines**: Preparation instructions

### 5. Formula Management (验方管理)
- **Classic Formulas**: Traditional prescription templates
- **Personal Formulas**: Doctor's custom formulas
- **Formula Categories**: Organization by treatment type
- **Quick Selection**: Rapid formula application

### 6. Medical Case Management (医案管理)
- **Case Creation**: New consultation records
- **Progress Tracking**: Treatment outcome monitoring
- **Case Templates**: Reusable case structures
- **Case Analysis**: Treatment effectiveness review

### 7. User & Access Management (用户权限管理)
- **Role-Based Access**: Admin, Doctor, Receptionist roles
- **Authentication**: Secure login with JWT tokens
- **Permission Control**: Feature-level access control
- **Audit Trail**: User action logging

### 8. Reporting & Analytics (报表分析)
- **Patient Statistics**: Demographics and visit patterns
- **Prescription Analysis**: Most used herbs and formulas
- **Revenue Reports**: Financial summaries
- **Treatment Outcomes**: Success rate tracking

## Use Cases

### Primary Use Cases

1. **New Patient Registration**
   - Receptionist creates new patient profile
   - Collects demographic and contact information
   - Records medical history and allergies
   - Assigns patient ID for tracking

2. **TCM Consultation Process**
   - Doctor retrieves patient record
   - Performs four diagnostic methods
   - Records symptoms and observations
   - Makes syndrome differentiation
   - Documents treatment principle

3. **Prescription Creation**
   - Select herbs from database
   - Apply formula template if applicable
   - Adjust dosages based on patient condition
   - Check for herb interactions
   - Generate and print prescription

4. **Follow-up Visit**
   - Retrieve previous consultation records
   - Compare current symptoms with previous
   - Adjust treatment based on progress
   - Create new or modified prescription

5. **Herb Inventory Check**
   - Review available herbs
   - Check pricing information
   - Update herb details as needed
   - Ensure prescription availability

### Secondary Use Cases

1. **Report Generation**
   - Generate patient visit reports
   - Create prescription statistics
   - Export data for analysis

2. **System Administration**
   - Add/modify user accounts
   - Configure system settings
   - Backup and restore data
   - Monitor system usage

## Business Rules

### Clinical Rules
1. **Prescription Limits**:
   - Maximum 30 herbs per prescription
   - Dosage must be within safe ranges
   - Toxic herbs require special marking

2. **Consultation Requirements**:
   - Must have active patient record
   - Four diagnostics should be documented
   - Syndrome differentiation required before prescription

3. **Medical Records**:
   - Cannot delete consultation records
   - Modifications create audit trail
   - Records retained minimum 15 years

### Administrative Rules
1. **User Access**:
   - Doctors can only modify own consultations
   - Admins have full system access
   - Receptionists limited to registration

2. **Data Integrity**:
   - Patient ID unique and immutable
   - Prescription numbers sequential
   - Timestamps automatic and unmodifiable

## Compliance Requirements

### Healthcare Regulations
- **Medical Record Standards**: Comply with Chinese medical record regulations
- **Prescription Format**: Follow TCM prescription standards
- **Data Privacy**: Patient information protection
- **Audit Requirements**: Complete action logging

### TCM Specific Standards
- **Herb Nomenclature**: Use standard Chinese herb names
- **Dosage Guidelines**: Follow Chinese Pharmacopoeia standards
- **Formula Standards**: Classic formula accuracy
- **Diagnostic Terminology**: Standard TCM terminology

## Product Differentiators

1. **TCM-Specific Design**: Built specifically for TCM practices, not generic medical software
2. **Integrated Workflow**: Seamless consultation to prescription process
3. **Formula Intelligence**: Smart formula suggestions and modifications
4. **Herb Compatibility**: Automatic interaction checking
5. **Professional Output**: Properly formatted TCM prescriptions
6. **Lightweight Deployment**: Suitable for small clinic infrastructure

## Success Criteria

### User Satisfaction
- Reduce consultation time by 30%
- Eliminate prescription calculation errors
- Improve patient record accessibility
- Increase prescription accuracy

### Business Impact
- Digital transformation of paper processes
- Improved clinic efficiency
- Better patient care documentation
- Regulatory compliance achievement

### Technical Success
- System uptime > 99.5%
- Response time < 2 seconds
- Zero data loss incidents
- Successful daily backups

## Future Enhancements (Planned)

1. **Phase 2 Features**:
   - Appointment scheduling system
   - Inventory management with stock tracking
   - Financial management module
   - SMS/WeChat notifications

2. **Phase 3 Features**:
   - Telemedicine support
   - AI-assisted diagnosis suggestions
   - Mobile app for doctors
   - Cloud synchronization

3. **Long-term Vision**:
   - Multi-clinic chain support
   - Integration with insurance systems
   - Research data analytics
   - TCM education modules