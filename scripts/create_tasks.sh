#!/bin/bash
cd D:/source/repos/LYBTZYZS

# Task 002
sed '1,/^---$/d; /^---$/q' .claude/epics/prism-8x-refactor-plan/002.md | sed '1,/^---$/d' > /tmp/task002.md
BODY=$(cat /tmp/task002.md)
gh sub-issue create --parent 747 --title "Standardize Navigation with IRegionManager" --body "$BODY"
echo ".claude/epics/prism-8x-refactor-plan/002.md:ISSUE" >> /tmp/task-mapping.txt

# Task 003
sed '1,/^---$/d; /^---$/q' .claude/epics/prism-8x-refactor-plan/003.md | sed '1,/^---$/d' > /tmp/task003.md
BODY=$(cat /tmp/task003.md)
gh sub-issue create --parent 747 --title "Refactor Dialog System to ICustomDialogAware" --body "$BODY"
echo ".claude/epics/prism-8x-refactor-plan/003.md:ISSUE" >> /tmp/task-mapping.txt

# Task 004
sed '1,/^---$/d; /^---$/q' .claude/epics/prism-8x-refactor-plan/004.md | sed '1,/^---$/d' > /tmp/task004.md
BODY=$(cat /tmp/task004.md)
gh sub-issue create --parent 747 --title "Unify ViewModel Base Classes with Prism Standards" --body "$BODY"
echo ".claude/epics/prism-8x-refactor-plan/004.md:ISSUE" >> /tmp/task-mapping.txt

# Task 005
sed '1,/^---$/d; /^---$/q' .claude/epics/prism-8x-refactor-plan/005.md | sed '1,/^---$/d' > /tmp/task005.md
BODY=$(cat /tmp/task005.md)
gh sub-issue create --parent 747 --title "Eliminate Service Locator Anti-Pattern" --body "$BODY"
echo ".claude/epics/prism-8x-refactor-plan/005.md:ISSUE" >> /tmp/task-mapping.txt

# Task 006
sed '1,/^---$/d; /^---$/q' .claude/epics/prism-8x-refactor-plan/006.md | sed '1,/^---$/d' > /tmp/task006.md
BODY=$(cat /tmp/task006.md)
gh sub-issue create --parent 747 --title "Migrate Commands to Reactive Patterns" --body "$BODY"
echo ".claude/epics/prism-8x-refactor-plan/006.md:ISSUE" >> /tmp/task-mapping.txt

# Task 007
sed '1,/^---$/d; /^---$/q' .claude/epics/prism-8x-refactor-plan/007.md | sed '1,/^---$/d' > /tmp/task007.md
BODY=$(cat /tmp/task007.md)
gh sub-issue create --parent 747 --title "Clean and Consolidate Module Registration" --body "$BODY"
echo ".claude/epics/prism-8x-refactor-plan/007.md:ISSUE" >> /tmp/task-mapping.txt

# Task 008
sed '1,/^---$/d; /^---$/q' .claude/epics/prism-8x-refactor-plan/008.md | sed '1,/^---$/d' > /tmp/task008.md
BODY=$(cat /tmp/task008.md)
gh sub-issue create --parent 747 --title "Testing, Validation and Documentation" --body "$BODY"
echo ".claude/epics/prism-8x-refactor-plan/008.md:ISSUE" >> /tmp/task-mapping.txt