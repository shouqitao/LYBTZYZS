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
