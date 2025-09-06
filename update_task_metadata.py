#!/usr/bin/env python3
"""Script to update task file metadata with GitHub information"""

import os
import re

# Mapping of task numbers to GitHub issue IDs
task_mapping = {
    "002": "548",
    "003": "549", 
    "004": "550",
    "005": "551",
    "006": "552",
    "007": "553",
    "008": "554",
    "009": "555",
    "010": "556"
}

epic_id = "546"
epic_url = "https://github.com/shouqitao/LYBTZYZS/issues/546"

def update_task_file(task_num, github_id):
    """Update a single task file with GitHub metadata"""
    file_path = f".claude/epics/frontend-logic-refactor/{github_id}.md"
    
    if not os.path.exists(file_path):
        print(f"File {file_path} not found, skipping")
        return
    
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Find the frontmatter and update it
    frontmatter_match = re.match(r'^---\n(.*?)\n---\n(.*)', content, re.DOTALL)
    if not frontmatter_match:
        print(f"No frontmatter found in {file_path}")
        return
    
    frontmatter = frontmatter_match.group(1)
    body = frontmatter_match.group(2)
    
    # Parse existing frontmatter
    lines = frontmatter.split('\n')
    new_lines = []
    
    for line in lines:
        if line.startswith('task:'):
            new_lines.append(line)
            new_lines.append(f'github_id: {github_id}')
            new_lines.append(f'github_url: https://github.com/shouqitao/LYBTZYZS/issues/{github_id}')
            new_lines.append(f'epic_id: {epic_id}')
            new_lines.append(f'epic_url: {epic_url}')
        elif not line.startswith(('github_id:', 'github_url:', 'epic_id:', 'epic_url:', 'status:')):
            new_lines.append(line)
    
    # Add status if not present
    if not any(line.startswith('status:') for line in new_lines):
        new_lines.append('status: backlog')
    
    # Write updated content
    new_content = f"---\n{chr(10).join(new_lines)}\n---\n{body}"
    
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(new_content)
    
    print(f"Updated {file_path}")

# Update all task files
for task_num, github_id in task_mapping.items():
    update_task_file(task_num, github_id)

print("All task files updated with GitHub metadata")