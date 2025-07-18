# Quick Add Herbs Feature Design

This document describes the layout and interaction for the **Quick Add Herbs** capability used in the traditional Chinese medicine prescription module. The goal is rapid batch entry of herbs while keeping the main table editable.

## 1. Layout Overview

The quick add interface appears below the main herb combination table and contains two parts:

1. **Herb Area** – shows a list of herbs ready to be imported.
2. **Search Area** – fuzzy search input to select herbs from the master library.

## 2. Search Area Functionality

- The search input performs fuzzy matching as the user types a herb name, pinyin or initials. Matching herbs from the master library are displayed in a suggestion list.
- A herb can be chosen via keyboard (arrow keys/Tab/Enter) or mouse. Confirmation (Enter or click) adds the herb to the pending list and clears the input for more entries.
- Multiple herbs may be added in sequence to form a batch list. If no herbs match, a prompt such as “No such herb in the database” is shown.

## 3. Herb Area

- Pending herbs are presented as removable tags or list items. Users may delete individual entries before import.
- Selecting **Import** adds all pending herbs to the main table in batch.
- Duplicate detection ensures herbs already present are ignored or merged. After a successful import the pending list is cleared and the main table becomes editable for dosage or other fields.

## 4. Keyboard and Mouse Support

Full keyboard and mouse controls exist for navigating suggestions, selecting items, confirming additions and deleting from the pending list, enabling efficient clinical usage.

## 5. Integration with the Main Herb Table

- Imported herbs behave the same as manually added entries. The table supports editing of dosage, unit and effect, and allows row deletion.
- No duplicate herbs remain in the table after import; duplicates are either merged or ignored.

## 6. Usability and Performance

This feature focuses on responsive linkage with the herb library for rapid entry. It is ideal for pharmacy or treatment-room workflows where speed and accuracy are essential.
