# 模块对接分析报告：患者管理 (Patients)

| 统计项 | 数量 |
| :--- | :---: |
| WebApi 总点数 | 10 |
| Desktop 已对接点数 | 5 |
| **对接完成情况** | **50%** |

---

### WebApi 总点数列表 (10 个)

- `GetList` (GET /api/v1/patients)
- `GetById` (GET /api/v1/patients/{id})
- `Add` (POST /api/v1/patients)
- `Update` (PUT /api/v1/patients/{id})
- `Delete` (DELETE /api/v1/patients/{id})
- `Enable` (POST /api/v1/patients/{id}/enable)
- `Disable` (POST /api/v1/patients/{id}/disable)
- `GetByIdCard` (GET /api/v1/patients/by-idcard/{idCard})
- `GetByPhone` (GET /api/v1/patients/by-phone/{phone})
- `Search` (GET /api/v1/patients/search)

---

### Desktop 已对接点数列表 (5 个)

- `CreatePatientAsync`
- `UpdatePatientAsync`
- `DeletePatientAsync`
- `ToggleStatusAsync` (后端对应 `Enable` 和 `Disable` 两个端点)

---

### 未对接点分析

- **`GetList`**: 分页获取患者列表。
- **`GetById`**: 获取单个患者的详细信息。
- **`GetByIdCard`**: 通过身份证号查询。
- **`GetByPhone`**: 通过电话号码查询。
- **`Search`**: 关键词模糊搜索。

---

### 状态总结

**写操作已对接，读操作未对接。**

前端的 `PatientBusinessService` 已经完成了对患者档案的增、删、改操作。但 `PatientQueryService` 是一个“空壳”，所有查询类API（列表、搜索、详情）都没有被调用，导致前端的患者列表和搜索功能目前无法使用真实数据。
