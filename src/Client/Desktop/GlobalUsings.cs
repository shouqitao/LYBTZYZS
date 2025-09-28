// ========================================
// Desktop客户端层全局using声明
// 用于减少重复的命名空间引用
// Issue #787: 代码清理第一阶段
// ========================================

global using System;
global using System.Collections.Generic;
global using System.Collections.ObjectModel;
global using System.ComponentModel;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
global using System.Windows.Input;

// Microsoft Extensions
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Configuration;

// Prism框架
global using Prism.Commands;
global using Prism.Mvvm;
global using Prism.Regions;
global using Prism.Services.Dialogs;
global using Prism.Ioc;
global using Prism.Modularity;

// 项目内部共享
global using LYBT.Shared.Models.Common;
global using LYBT.Shared.Models.Contracts;