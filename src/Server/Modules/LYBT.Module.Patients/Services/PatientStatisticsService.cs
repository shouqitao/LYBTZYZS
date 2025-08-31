using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Entities.Patients;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Patients.Services
{
    /// <summary>
    /// 患者统计服务
    /// 负责所有与患者统计分析相关的功能
    /// </summary>
    public class PatientStatisticsService
    {
        private readonly IPatientRepository _patientRepository;
        private readonly IMapper _mapper;

        public PatientStatisticsService(IPatientRepository patientRepository, IMapper mapper)
        {
            _patientRepository = patientRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// 获取患者统计信息
        /// </summary>
        public async Task<PatientStatisticsDto> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var allPatients = await _patientRepository.GetAllAsync();
            var now = DateTime.Now;
            var today = DateTime.Today;

            return new PatientStatisticsDto
            {
                TotalPatients = allPatients.Count(),
                ActivePatients = allPatients.Count(p => p.Status == CommonStatus.Enabled),
                InactivePatients = allPatients.Count(p => p.Status == CommonStatus.Disabled),
                MalePatients = allPatients.Count(p => p.Gender == Gender.Male),
                FemalePatients = allPatients.Count(p => p.Gender == Gender.Female),
                AverageAge = allPatients.Any() ? (decimal)(allPatients.Average(p => p.Age) ?? 0) : 0,
                TotalVisits = allPatients.Sum(p => p.VisitCount),
                AverageVisits = allPatients.Any() ? (decimal)allPatients.Average(p => p.VisitCount) : 0,
                PatientsWithAllergy = allPatients.Count(p => !string.IsNullOrEmpty(p.AllergyHistory)),
                // UltraThink v2.0: 使用LastVisitTime替代已删除的CreateTime
                TodayNewPatients = allPatients.Count(p => p.LastVisitTime.HasValue && p.LastVisitTime.Value.Date == today),
                NewPatients = allPatients.Count(p =>
                    p.LastVisitTime.HasValue &&
                    (!startDate.HasValue || p.LastVisitTime.Value >= startDate) &&
                    (!endDate.HasValue || p.LastVisitTime.Value <= endDate)),
                LostPatients = allPatients.Count(p => p.LastVisitTime.HasValue &&
                    (now - p.LastVisitTime.Value).TotalDays > PatientConstants.InactivePatientsDefaultDays)
            };
        }

        /// <summary>
        /// 获取患者年龄分布统计
        /// </summary>
        public async Task<List<AgeDistributionDto>> GetAgeDistributionAsync()
        {
            var patients = await _patientRepository.GetActivePatientsAsync();
            var total = patients.Count;

            return PatientConstants.AgeRanges.Select(range =>
            {
                var patientsInRange = patients.Where(p => p.Age >= range.Min && p.Age <= range.Max).ToList();
                return new AgeDistributionDto
                {
                    AgeRange = range.Range,
                    MinAge = range.Min,
                    MaxAge = range.Max == int.MaxValue ? 100 : range.Max,
                    PatientCount = patientsInRange.Count,
                    Percentage = total > 0 ? (decimal)patientsInRange.Count / total * 100 : 0,
                    MaleCount = patientsInRange.Count(p => p.Gender == Gender.Male),
                    FemaleCount = patientsInRange.Count(p => p.Gender == Gender.Female)
                };
            }).ToList();
        }

        /// <summary>
        /// 获取患者性别分布统计
        /// </summary>
        public async Task<GenderDistributionDto> GetGenderDistributionAsync()
        {
            var patients = await _patientRepository.GetActivePatientsAsync();
            var total = patients.Count;

            var maleCount = patients.Count(p => p.Gender == Gender.Male);
            var femaleCount = patients.Count(p => p.Gender == Gender.Female);
            var unknownCount = patients.Count(p => p.Gender == Gender.Unknown);

            return new GenderDistributionDto
            {
                MaleCount = maleCount,
                MalePercentage = total > 0 ? (decimal)maleCount / total * 100 : 0,
                FemaleCount = femaleCount,
                FemalePercentage = total > 0 ? (decimal)femaleCount / total * 100 : 0,
                UnknownCount = unknownCount,
                UnknownPercentage = total > 0 ? (decimal)unknownCount / total * 100 : 0,
                TotalCount = total
            };
        }

        /// <summary>
        /// 获取新增患者趋势
        /// </summary>
        public async Task<List<PatientTrendDto>> GetNewPatientTrendAsync(int months = PatientConstants.DefaultStatisticsMonths)
        {
            var patients = await _patientRepository.GetAllAsync();
            var startDate = DateTime.Now.AddMonths(-months);

            // UltraThink v2.0: 使用LastVisitTime替代已删除的CreateTime
            var monthlyData = patients
                .Where(p => p.LastVisitTime.HasValue && p.LastVisitTime.Value >= startDate)
                .GroupBy(p => new { Year = p.LastVisitTime!.Value.Year, Month = p.LastVisitTime!.Value.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g =>
                {
                    var monthPatients = g.ToList();
                    return new PatientTrendDto
                    {
                        Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                        MonthName = $"{g.Key.Year}年{g.Key.Month}月",
                        NewPatients = monthPatients.Count,
                        VisitCount = monthPatients.Sum(p => p.VisitCount),
                        MaleCount = monthPatients.Count(p => p.Gender == Gender.Male),
                        FemaleCount = monthPatients.Count(p => p.Gender == Gender.Female),
                        GrowthRate = 0 // 计算环比增长率
                    };
                }).ToList();

            // 计算环比增长率
            CalculateGrowthRates(monthlyData);

            return monthlyData;
        }

        /// <summary>
        /// 获取活跃患者列表
        /// </summary>
        public async Task<List<PatientDto>> GetRecentActivePatientsAsync(int days = PatientConstants.ActivePatientsDefaultDays)
        {
            var cutoffDate = DateTime.Now.AddDays(-days);
            var patients = await _patientRepository.GetActivePatientsAsync();
            var activePatients = patients
                .Where(p => p.LastVisitTime.HasValue && p.LastVisitTime.Value >= cutoffDate)
                .OrderByDescending(p => p.LastVisitTime)
                .ToList();

            return activePatients.Select(_mapper.Map<PatientDto>).ToList();
        }

        /// <summary>
        /// 获取流失患者列表
        /// </summary>
        public async Task<List<PatientDto>> GetInactivePatientsAsync(int days = PatientConstants.InactivePatientsDefaultDays)
        {
            var cutoffDate = DateTime.Now.AddDays(-days);
            var patients = await _patientRepository.GetActivePatientsAsync();
            var inactivePatients = patients
                .Where(p => !p.LastVisitTime.HasValue || p.LastVisitTime.Value < cutoffDate)
                .OrderBy(p => p.LastVisitTime ?? DateTime.MinValue)
                .ToList();

            return inactivePatients.Select(_mapper.Map<PatientDto>).ToList();
        }

        /// <summary>
        /// 获取今日新增患者
        /// </summary>
        public async Task<List<PatientDto>> GetTodayNewPatientsAsync()
        {
            var today = DateTime.Today;
            var patients = await _patientRepository.GetActivePatientsAsync();
            // UltraThink v2.0: 使用LastVisitTime替代已删除的CreateTime
            var todayPatients = patients
                .Where(p => p.LastVisitTime.HasValue && p.LastVisitTime.Value.Date == today)
                .OrderByDescending(p => p.LastVisitTime)
                .ToList();

            return todayPatients.Select(_mapper.Map<PatientDto>).ToList();
        }

        /// <summary>
        /// 计算环比增长率
        /// </summary>
        private void CalculateGrowthRates(List<PatientTrendDto> monthlyData)
        {
            for (int i = 1; i < monthlyData.Count; i++)
            {
                if (monthlyData[i - 1].NewPatients > 0)
                {
                    monthlyData[i].GrowthRate =
                        (decimal)(monthlyData[i].NewPatients - monthlyData[i - 1].NewPatients) /
                        monthlyData[i - 1].NewPatients * 100;
                }
            }
        }
    }
}