// ========================================
// Server层全局using声明
// 用于减少重复的命名空间引用
// Issue #787: 代码清理第一阶段 + GlobalUsings优化
// ========================================

// System 核心命名空间 (高频使用)
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
global using System.ComponentModel;
global using System.ComponentModel.DataAnnotations;
global using System.ComponentModel.DataAnnotations.Schema;
global using System.Text;
global using System.Security.Claims;
global using System.Linq.Expressions;
global using System.Collections.Concurrent;
global using System.Text.Json;
global using System.Reflection;

// Microsoft Extensions
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.Options;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Caching.Memory;

// Entity Framework Core
global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Migrations;

// ASP.NET Core
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Http;

// 身份认证与安全
global using Microsoft.IdentityModel.Tokens;
global using System.IdentityModel.Tokens.Jwt;
global using System.Security.Cryptography;

// 项目内部共享 (高频使用)
global using LYBT.Infrastructure.Data;
global using LYBT.Shared.Models.Contracts.Common;
global using LYBT.Shared.Models.Enums;
global using LYBT.Server.Interfaces.Services;
global using LYBT.Entities.Common;
global using LYBT.Infrastructure.Interfaces;
global using LYBT.Infrastructure.Repositories;