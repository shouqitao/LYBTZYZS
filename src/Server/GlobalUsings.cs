// ========================================
// Server层全局using声明
// 用于减少重复的命名空间引用
// Issue #787: 代码清理第一阶段
// ========================================

global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;

// Microsoft Extensions
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.Options;

// Entity Framework Core
global using Microsoft.EntityFrameworkCore;

// ASP.NET Core
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Authorization;

// 项目内部共享
global using LYBT.Shared.Models.Common;
global using LYBT.Infrastructure.Data;