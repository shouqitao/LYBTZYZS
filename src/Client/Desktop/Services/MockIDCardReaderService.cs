using System;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Services
{

    /// <summary>
    /// 模拟身份证读卡器服务
    /// 用于开发和测试，实际部署时替换为真实的读卡器服务
    /// </summary>
    public class MockIDCardReaderService : IIDCardReaderService
    {
        private readonly ILogger<MockIDCardReaderService> _logger;
        private IDCardReaderStatus _currentStatus = IDCardReaderStatus.Disconnected;
        private readonly Random _random = new Random();

        public event EventHandler<IDCardReaderStatusChangedEventArgs>? StatusChanged;

        public event EventHandler<IDCardReadEventArgs>? CardRead;

        public MockIDCardReaderService(ILogger<MockIDCardReaderService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> IsConnectedAsync()
        {
            await Task.Delay(100); // 模拟异步操作
            return _currentStatus == IDCardReaderStatus.Connected;
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                _logger.LogInformation("尝试连接模拟身份证读卡器...");

                UpdateStatus(IDCardReaderStatus.Connecting);

                // 模拟连接延迟
                await Task.Delay(1000);

                // 模拟连接成功
                UpdateStatus(IDCardReaderStatus.Connected);

                _logger.LogInformation("模拟身份证读卡器连接成功");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "连接模拟身份证读卡器失败");
                UpdateStatus(IDCardReaderStatus.Error);
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                _logger.LogInformation("断开模拟身份证读卡器连接...");

                await Task.Delay(500);

                UpdateStatus(IDCardReaderStatus.Disconnected);

                _logger.LogInformation("模拟身份证读卡器已断开");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "断开模拟身份证读卡器失败");
            }
        }

        public async Task<IDCardInfo?> ReadCardAsync()
        {
            try
            {
                if (_currentStatus != IDCardReaderStatus.Connected)
                {
                    _logger.LogWarning("读卡器未连接，无法读取身份证");
                    OnCardRead(false, "读卡器未连接");
                    return null;
                }

                _logger.LogInformation("开始读取模拟身份证信息...");
                UpdateStatus(IDCardReaderStatus.Reading);

                // 模拟读卡延迟
                await Task.Delay(2000);

                // 生成模拟数据
                var mockData = GenerateMockIDCardInfo();

                UpdateStatus(IDCardReaderStatus.Connected);

                _logger.LogInformation($"成功读取模拟身份证信息: {mockData.Name}");

                OnCardRead(true, null, mockData);

                return mockData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取模拟身份证信息失败");
                UpdateStatus(IDCardReaderStatus.Error);
                OnCardRead(false, ex.Message);
                return null;
            }
        }

        private IDCardInfo GenerateMockIDCardInfo()
        {
            var testData = GetRandomTestData();

            return new IDCardInfo
            {
                Name = testData.Name,
                Gender = testData.Gender,
                Nation = "汉",
                BirthDate = testData.BirthDate,
                IDNumber = testData.IDNumber,
                Address = testData.Address,
                IssuingAuthority = "某市公安局",
                ValidFrom = DateTime.Today.AddYears(-10),
                ValidTo = DateTime.Today.AddYears(10),
                PhotoBase64 = null // 模拟数据不包含照片
            };
        }

        private (string Name, string Gender, DateTime BirthDate, string IDNumber, string Address) GetRandomTestData()
        {
            var testDataList = new[]
            {
                ("张三", "男", new DateTime(1985, 3, 15), "110101198503150012", "北京市东城区某街道123号"),
                ("李四", "女", new DateTime(1990, 7, 20), "310101199007200028", "上海市黄浦区某路456号"),
                ("王五", "男", new DateTime(1978, 12, 8), "440101197812080039", "广州市越秀区某巷789号"),
                ("赵六", "女", new DateTime(1995, 5, 25), "330101199505250045", "杭州市西湖区某街321号"),
                ("钱七", "男", new DateTime(1982, 9, 10), "320101198209100051", "南京市玄武区某路654号"),
                ("孙八", "女", new DateTime(1988, 11, 30), "510101198811300067", "成都市锦江区某街987号"),
                ("周九", "男", new DateTime(1975, 6, 18), "420101197506180073", "武汉市江汉区某路147号"),
                ("吴十", "女", new DateTime(1992, 2, 14), "500101199202140089", "重庆市渝中区某街258号")
            };

            return testDataList[_random.Next(testDataList.Length)];
        }

        private void UpdateStatus(IDCardReaderStatus newStatus)
        {
            if (_currentStatus != newStatus)
            {
                var oldStatus = _currentStatus;
                _currentStatus = newStatus;

                StatusChanged?.Invoke(this, new IDCardReaderStatusChangedEventArgs
                {
                    OldStatus = oldStatus,
                    NewStatus = newStatus,
                    Message = GetStatusMessage(newStatus)
                });
            }
        }

        private string GetStatusMessage(IDCardReaderStatus status)
        {
            return status switch
            {
                IDCardReaderStatus.Disconnected => "读卡器未连接",
                IDCardReaderStatus.Connecting => "正在连接读卡器...",
                IDCardReaderStatus.Connected => "读卡器已就绪",
                IDCardReaderStatus.Reading => "正在读取身份证...",
                IDCardReaderStatus.Error => "读卡器错误",
                _ => "未知状态"
            };
        }

        private void OnCardRead(bool success, string? errorMessage = null, IDCardInfo? cardInfo = null)
        {
            CardRead?.Invoke(this, new IDCardReadEventArgs
            {
                Success = success,
                ErrorMessage = errorMessage,
                CardInfo = cardInfo
            });
        }
    }
}
