# Herb Combination Editor Control

This folder contains `HerbCombinationEditorControl`, a reusable WPF user control
for editing formula templates and clinical prescriptions.

## Basic Usage

```xaml
<controls:HerbCombinationEditorControl
    Mode="Template"
    ShowFormulaName="True"
    HerbItems="{Binding Herbs}"
    FormulaName="{Binding TemplateName}"
    SaveCommand="{Binding SaveCmd}" />
```

Set `Mode` to `Template` to display the formula name input. In `Prescription`
mode the name field is hidden. Bind `HerbItems` to an
`ObservableCollection<HerbCombinationItem>` for editing.
Use `ReadOnly="True"` to disable editing.

## Interaction Design

The herb entry table uses a DataGrid and supports full keyboard operation for fast batch input.

1. A blank row is always present at the end of the grid so the user can start typing immediately.
2. When editing the **herb name** cell:
   - If the typed name does not exist in the master herb list, the editor should display a message: `"No such herb found in the database."`.
   - When the input uniquely matches one herb (by full name or pinyin code), the herb is auto-selected. Pressing **Enter** confirms the choice and moves focus to the dosage cell.
   - If multiple matches are found, a dropdown list appears. The user can cycle through candidates with **Tab** or the **Up/Down** keys. Pressing **Enter** confirms the current selection and also moves focus to the dosage field.
3. After both **herb name** and **dosage** in the current row are filled, pressing **Enter** on the last editable cell automatically creates a new blank row and focuses the herb name field in that row.
4. All interactions are designed to be completed with the keyboard without requiring the mouse.
5. The workflow repeats for each new row to maximise entry efficiency.

