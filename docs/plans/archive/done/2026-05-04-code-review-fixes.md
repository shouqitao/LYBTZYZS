# Code Review Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix 4 issues found during systematic code review of recent commits (HEAD~5..HEAD)

**Architecture:** Minor fixes across Desktop UI (XAML), Herb import handler, and test infrastructure. No architectural changes.

**Tech Stack:** WPF/XAML, C# .NET 8, FluentAssertions, Newman CLI

---

## File Structure

| File | Change Type | Responsibility |
|------|-------------|----------------|
| `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseEditControl.xaml` | Modify | PresentIllness MinHeight fix + Remark char counter |
| `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/ViewModels/Handlers/HerbImportExportHandler.cs` | Modify | DuplicateStrategy user prompt |
| `scripts/run-postman-tests.ps1` | Create | CI-integrated Newman runner |
| `scripts/run-postman-tests.bat` | Create | Batch wrapper for Newman runner |

---

### Task 1: Fix PresentIllness ComboBox MinHeight

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseEditControl.xaml:319`

**Context:** The PresentIllness ComboBox has `MinHeight="60"` which is unusually tall. Other ComboBoxes in the same file use `MinHeight="28"` (line 502) or no explicit MinHeight. This creates visual inconsistency.

- [ ] **Step 1: Change MinHeight from 60 to 32**

In `MedicalCaseEditControl.xaml`, line 319, change:
```xml
MinHeight="60"
```
to:
```xml
MinHeight="32"
```

- [ ] **Step 2: Build to verify XAML compiles**

Run: `dotnet build src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/LYBT.Desktop.MedicalCase.csproj --no-restore`
Expected: Build succeeded, 0 errors

- [ ] **Step 3: Commit**

```bash
git add src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseEditControl.xaml
git commit -m "fix(medicalcase): Reduce PresentIllness ComboBox MinHeight from 60 to 32 for visual consistency"
```

---

### Task 2: Add Remark Character Counter

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseEditControl.xaml:535-546`

**Context:** The design doc (`medicalcase-workspace-ui-optimization-plan.md` Section 6) specifies "超过500字提示" for the Remark field. Currently the field has `ValidationConstants.RemarkMaxLength = 1000` as a hard limit via `StringLength` attribute, but no visual feedback on character count. Adding a character counter gives users progressive feedback. The existing `ValidatingTextBoxStyle` with `INotifyDataErrorInfo` handles the 1000-char hard limit; the counter supplements this with at-a-glance length awareness.

- [ ] **Step 1: Add x:Name to Remark TextBox and add character counter below it**

In `MedicalCaseEditControl.xaml`, replace lines 535-546:

```xml
            <!-- 备注 - W2-3: 统一 Remark 数据源 -->
            <Border Grid.Row="6" Style="{StaticResource CompactSectionBorderStyle}" Margin="0,0,0,12">
                <StackPanel>
                    <TextBlock Text="备注" FontWeight="SemiBold" FontSize="13"
                               Foreground="{DynamicResource PrimaryTextBrush}" Margin="0,0,0,4"/>
                    <TextBox TabIndex="15"
                             Text="{Binding Remark, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
                             Style="{DynamicResource ValidatingTextBoxStyle}"
                             TextWrapping="Wrap" MinHeight="40" AcceptsReturn="True"
                             MaxHeight="120"/>
                </StackPanel>
            </Border>
```

With:

```xml
            <!-- 备注 - W2-3: 统一 Remark 数据源 -->
            <Border Grid.Row="6" Style="{StaticResource CompactSectionBorderStyle}" Margin="0,0,0,12">
                <StackPanel>
                    <TextBlock Text="备注" FontWeight="SemiBold" FontSize="13"
                               Foreground="{DynamicResource PrimaryTextBrush}" Margin="0,0,0,4"/>
                    <TextBox TabIndex="15"
                             x:Name="RemarkTextBox"
                             Text="{Binding Remark, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
                             Style="{DynamicResource ValidatingTextBoxStyle}"
                             TextWrapping="Wrap" MinHeight="40" AcceptsReturn="True"
                             MaxHeight="120"/>
                    <TextBlock FontSize="11" HorizontalAlignment="Right" Margin="0,2,0,0"
                               Foreground="{DynamicResource SecondaryTextBrush}">
                        <TextBlock.Text>
                            <MultiBinding StringFormat="{}{0}/1000">
                                <Binding Path="Text.Length" ElementName="RemarkTextBox" FallbackValue="0"/>
                            </MultiBinding>
                        </TextBlock.Text>
                    </TextBlock>
                </StackPanel>
            </Border>
```

- [ ] **Step 2: Build to verify XAML compiles**

Run: `dotnet build src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/LYBT.Desktop.MedicalCase.csproj --no-restore`
Expected: Build succeeded, 0 errors

- [ ] **Step 3: Commit**

```bash
git add src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseEditControl.xaml
git commit -m "feat(medicalcase): Add Remark character counter (X/1000) per design doc Section 6"
```

---

### Task 3: Make DuplicateStrategy User-Configurable in Herb Import

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/ViewModels/Handlers/HerbImportExportHandler.cs:62-66`

**Context:** The herb import handler hardcodes `DuplicateStrategy.Skip`. Offering the user a choice between "跳过重复" (Skip) and "覆盖重复" (Overwrite) is better UX. The `ICommonDialogService.ShowConfirmAsync` method provides a simple two-choice prompt. The dialog is shown before the import API call, outside the loading overlay callback, to avoid the UI layering issue flagged in the code review.

- [ ] **Step 1: Add duplicate strategy prompt before import**

In `HerbImportExportHandler.cs`, replace lines 62-66:

```csharp
                var request = new HerbBatchImportInputDto
                {
                    Herbs = herbs,
                    Strategy = DuplicateStrategy.Skip
                };
```

With:

```csharp
                var useOverwrite = await _masterDetailServices.Dialog.ShowConfirmAsync(
                    "检测到导入数据中可能包含重复药材。\n\n选择「是」覆盖已有记录\n选择「否」跳过重复记录",
                    "重复处理策略");
                var request = new HerbBatchImportInputDto
                {
                    Herbs = herbs,
                    Strategy = useOverwrite ? DuplicateStrategy.Overwrite : DuplicateStrategy.Skip
                };
```

- [ ] **Step 2: Build to verify compilation**

Run: `dotnet build src/Client/Desktop/Modules/LYBT.Desktop.Herbs/LYBT.Desktop.Herbs.csproj --no-restore`
Expected: Build succeeded, 0 errors

- [ ] **Step 3: Commit**

```bash
git add src/Client/Desktop/Modules/LYBT.Desktop.Herbs/ViewModels/Handlers/HerbImportExportHandler.cs
git commit -m "feat(herbs): Prompt user for duplicate strategy instead of hardcoded Skip"
```

---

### Task 4: Add CI-Integrated Postman Test Script

**Files:**
- Create: `scripts/run-postman-tests.ps1`
- Create: `scripts/run-postman-tests.bat`

**Context:** A `run-tests.ps1` exists in `tests/postman/` but there's no script in the `scripts/` directory (the project's standard automation location per CLAUDE.md). Adding a CI-friendly wrapper in `scripts/` ensures consistency with the project's script organization.

- [ ] **Step 1: Create the PowerShell script**

Create `scripts/run-postman-tests.ps1`:

```powershell
# Postman/Newman Integration Test Runner
# Usage: .\scripts\run-postman-tests.ps1
# Prerequisites: npm install -g newman newman-reporter-htmlextra

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$collection = Join-Path $projectRoot "tests\postman\local-api-tests.postman_collection.json"
$environment = Join-Path $projectRoot "tests\postman\local-api.environment.json"
$resultsDir = Join-Path $projectRoot "tests\postman\results"

if (!(Test-Path $collection)) {
    Write-Host "ERROR: Collection not found: $collection" -ForegroundColor Red
    exit 1
}

if (!(Test-Path $resultsDir)) {
    New-Item -ItemType Directory -Path $resultsDir | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$reportPath = Join-Path $resultsDir "report_$timestamp.html"

Write-Host "Running LocalWebAPI Postman tests..." -ForegroundColor Cyan
Write-Host "  Collection: $collection"
Write-Host "  Environment: $environment"
Write-Host ""

newman run $collection `
    -e $environment `
    --reporters cli,htmlextra `
    --reporter-htmlextra-export $reportPath `
    --timeout-request 10000 `
    --delay-request 100

$exitCode = $LASTEXITCODE

if ($exitCode -eq 0) {
    Write-Host "`nAll Postman tests passed!" -ForegroundColor Green
} else {
    Write-Host "`nSome Postman tests failed (exit code: $exitCode)" -ForegroundColor Red
}

Write-Host "Report: $reportPath"
exit $exitCode
```

- [ ] **Step 2: Create the batch wrapper**

Create `scripts/run-postman-tests.bat`:

```batch
@echo off
REM Postman/Newman Integration Test Runner
REM Usage: scripts\run-postman-tests.bat
REM Prerequisites: npm install -g newman newman-reporter-htmlextra

powershell -ExecutionPolicy Bypass -File "%~dp0run-postman-tests.ps1"
exit /b %errorlevel%
```

- [ ] **Step 3: Commit**

```bash
git add scripts/run-postman-tests.ps1 scripts/run-postman-tests.bat
git commit -m "feat(scripts): Add CI-integrated Postman/Newman test runner in scripts/"
```

---

## Verification

After all tasks are completed:

1. Build the full solution: `dotnet build LYBTZYZS.sln`
2. Run Desktop tests: `dotnet test tests/LYBT.Tests.Desktop/ --no-restore`
3. Verify XAML changes visually by running the Desktop client (if possible)
