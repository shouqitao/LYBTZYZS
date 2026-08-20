---
tags: [system, dashboard]
aliases: [文档健康, 健康检查]
---

# 📊 文档健康仪表板

> 自动更新。基于 Dataview 插件查询。

## 文档统计

```dataview
TABLE length(rows) AS "文件数"
FROM ""
WHERE file.name != "_health-dashboard" AND !startswith(file.name, "_")
GROUP BY file.folder
SORT length(rows) DESC
```

## 无标签文档

> 缺少 frontmatter tags 的文档可能需要补充分类。

```dataview
LIST
FROM ""
WHERE !file.tags AND file.name != "_health-dashboard" AND !startswith(file.name, "_")
SORT file.name ASC
```

## 标签使用统计

```dataview
TABLE length(rows) AS "使用次数"
FROM ""
WHERE file.tags
FLATTEN file.tags AS tag
GROUP BY tag
SORT length(rows) DESC
LIMIT 20
```

## 死端页面（无出链）

> 这些页面没有引用其他文档。考虑添加相关链接。

```dataview
LIST
FROM ""
WHERE length(file.outlinks) = 0 AND file.name != "_health-dashboard" AND !startswith(file.name, "_")
SORT file.name ASC
```

## 孤儿页面（无入链）

> 这些页面没有被其他文档引用。考虑从相关文档添加链接。

```dataview
LIST
FROM ""
WHERE length(file.inlinks) = 0 AND file.name != "_health-dashboard" AND !startswith(file.name, "_")
SORT file.name ASC
```
