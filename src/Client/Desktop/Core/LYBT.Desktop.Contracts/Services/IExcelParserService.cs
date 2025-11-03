using System.Data;
using LYBT.Desktop.Contracts.Models;

namespace LYBT.Desktop.Contracts.Services
{
    /// <summary>
    /// Excel解析服务接口（Issue #1781 Task 8 Phase 1）
    ///
    /// 设计目标：
    /// 1. 单一职责：专注于Excel文件解析和验证逻辑
    /// 2. 解耦ViewModel：将PatientImportWizardViewModel的195行验证逻辑提取为独立服务
    /// 3. 可测试性：服务可独立进行单元测试，提高代码质量
    ///
    /// 架构定位：
    /// - 功能分层：辅助层功能（数据导入辅助工具）
    /// - 查询层：ParseExcelFileAsync（读取Excel文件）
    /// - 辅助层：ValidateImportData（数据验证）、ValidateExcelFormat（格式验证）
    /// - 写入控制：仅负责数据解析和验证，不涉及数据库写入
    ///
    /// 符合SOLID原则：
    /// - S: 单一职责（仅Excel解析和验证）
    /// - O: 开闭原则（接口稳定，实现可扩展）
    /// - L: 里氏替换原则（任何实现都可替换）
    /// - I: 接口隔离原则（接口方法专注于Excel处理）
    /// - D: 依赖倒置原则（高层依赖抽象，低层实现抽象）
    /// </summary>
    public interface IExcelParserService
    {
        #region 1. Excel文件解析

        /// <summary>
        /// 解析Excel文件为DataTable
        /// 用途：读取Excel文件并转换为DataTable，支持.xlsx和.xls格式
        /// </summary>
        /// <param name="filePath">Excel文件路径</param>
        /// <returns>解析后的DataTable（第一个工作表）</returns>
        /// <exception cref="FileNotFoundException">文件不存在时抛出</exception>
        /// <exception cref="InvalidOperationException">Excel格式不正确时抛出</exception>
        Task<DataTable> ParseExcelFileAsync(string filePath);

        #endregion

        #region 2. 数据验证

        /// <summary>
        /// 验证导入数据的完整性和正确性
        /// 用途：对Excel数据进行全面验证，包括必需列、数据格式、重复检查等
        ///
        /// 验证规则：
        /// 1. 必需列检查：姓名、性别
        /// 2. 可选列检查：年龄、电话、证件号、地址、过敏史
        /// 3. 姓名验证：非空、最大50字符、重复检查
        /// 4. 性别验证：必须为"男"、"女"或"未知"
        /// 5. 年龄验证：数字格式、范围0-150
        /// 6. 电话验证：长度7-15字符、格式检查、重复检查
        /// 7. 证件号验证：长度15或18字符、重复检查
        /// 8. 地址验证：最大200字符
        /// 9. 过敏史验证：最大500字符
        /// </summary>
        /// <param name="dataTable">待验证的DataTable</param>
        /// <returns>验证结果（包含错误、警告、有效/无效行数）</returns>
        ImportValidationResult ValidateImportData(DataTable dataTable);

        /// <summary>
        /// 验证Excel文件格式
        /// 用途：检查文件扩展名和基本格式，快速失败验证
        /// </summary>
        /// <param name="filePath">Excel文件路径</param>
        /// <param name="errorMessage">错误信息（如果验证失败）</param>
        /// <returns>验证是否通过</returns>
        bool ValidateExcelFormat(string filePath, out string errorMessage);

        #endregion

        #region 3. 辅助功能

        /// <summary>
        /// 获取Excel文件支持的扩展名列表
        /// 用途：文件对话框过滤器设置
        /// </summary>
        /// <returns>支持的扩展名列表（如：.xlsx, .xls）</returns>
        IEnumerable<string> GetSupportedExtensions();

        /// <summary>
        /// 获取患者导入模板的列定义
        /// 用途：生成Excel模板文件时使用
        /// </summary>
        /// <returns>列名列表（按顺序：姓名、性别、年龄、电话、证件号、地址、过敏史）</returns>
        IEnumerable<string> GetTemplateColumns();

        #endregion
    }
}
