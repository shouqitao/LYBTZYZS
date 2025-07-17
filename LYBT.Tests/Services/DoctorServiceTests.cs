using System;
using AutoMapper;
using System.Threading.Tasks;
using Moq;
using Xunit;
using LYBT.Module.Doctors.Services;
using LYBT.Module.Doctors.Interfaces;
using LYBT.Module.Doctors.Dtos;
using LYBT.Module.Doctors.Mapping;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Models;
using LYBT.Models.Doctors;
using LYBT.Common.Enums;
using CommonUtil = LYBT.CommonUtils.CommonUtils;

namespace LYBT.Tests.Services;

public class DoctorServiceTests
{
    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DoctorMappingProfile>());
        return config.CreateMapper();
    }

    [Fact]
    public async Task AddAsync_CallsRepository()
    {
        var repo = new Mock<IDoctorRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<LYBT.Models.Doctors.DoctorModel>())).ReturnsAsync(true);
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new UserModel());
        var mapper = CreateMapper();
        var service = new DoctorService(repo.Object, userRepo.Object, mapper);

        var dto = new DoctorDetailDto { UserId = Guid.NewGuid() };
        var result = await service.AddAsync(dto);

        Assert.True(result);
        repo.Verify(r => r.AddAsync(It.IsAny<LYBT.Models.Doctors.DoctorModel>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_Throws_WhenUserMissing()
    {
        var repo = new Mock<IDoctorRepository>();
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((UserModel?)null);
        var mapper = CreateMapper();
        var service = new DoctorService(repo.Object, userRepo.Object, mapper);

        await Assert.ThrowsAsync<Exception>(() => service.AddAsync(new DoctorDetailDto { UserId = Guid.NewGuid() }));
    }

    [Fact]
    public async Task AddAsync_Throws_WhenDoctorExists()
    {
        var repo = new Mock<IDoctorRepository>();
        repo.Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>())).ReturnsAsync(new LYBT.Models.Doctors.DoctorModel());
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new UserModel());
        var mapper = CreateMapper();
        var service = new DoctorService(repo.Object, userRepo.Object, mapper);

        await Assert.ThrowsAsync<Exception>(() => service.AddAsync(new DoctorDetailDto { UserId = Guid.NewGuid() }));
    }

    [Fact]
    public async Task AddAsync_MapsAllFields()
    {
        DoctorModel? savedModel = null;
        var repo = new Mock<IDoctorRepository>();
        repo.Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>())).ReturnsAsync((DoctorModel?)null);
        repo.Setup(r => r.AddAsync(It.IsAny<DoctorModel>()))
            .Callback<DoctorModel>(m => savedModel = m)
            .ReturnsAsync(true);
        var userRepo = new Mock<IUserRepository>();
        var existingUser = new UserModel
        {
            RealName = "Test",
            PinyinCode = CommonUtil.GetPinyinCode("Test")
        };
        userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(existingUser);
        var mapper = CreateMapper();
        var service = new DoctorService(repo.Object, userRepo.Object, mapper);

        var dto = new DoctorDetailDto
        {
            UserId = Guid.NewGuid(),
            Gender = Gender.Female,
            Birthday = new DateTime(1980, 1, 1),
            Title = DoctorTitle.Senior,
            LicenseNumber = "LN",
            Specialty = "Spec",
            Status = DoctorStatus.Active,
            WorkStatus = DoctorWorkStatus.Clinic,
            PinyinCode = "ABC",
            Remark = "note",
            ContactNumber = "123"
        };

        var result = await service.AddAsync(dto);

        Assert.True(result);
        Assert.NotNull(savedModel);
        // User navigation property should be assigned with the existing user
        Assert.Same(existingUser, savedModel!.User);
        Assert.Equal(dto.Birthday, savedModel!.Birthday);
        Assert.Equal(dto.Title, savedModel.Title);
        Assert.Equal(dto.LicenseNumber, savedModel.LicenseNumber);
        Assert.Equal(dto.Specialty, savedModel.Specialty);
        Assert.Equal(dto.Status, savedModel.Status);
        Assert.Equal(dto.WorkStatus, savedModel.WorkStatus);
        var expectedCode = existingUser.PinyinCode;
        Assert.Equal(expectedCode, savedModel.PinyinCode);
        Assert.Equal(dto.Remark, savedModel.Remark);
        Assert.Equal(dto.ContactNumber, savedModel.ContactNumber);
        Assert.Equal(dto.Gender, savedModel.Gender);
    }

    [Fact]
    public async Task UpdateAsync_MapsAllFields()
    {
        var model = new DoctorModel
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            User = new UserModel
            {
                RealName = "Old",
                PinyinCode = CommonUtil.GetPinyinCode("Old")
            }
        };
        var repo = new Mock<IDoctorRepository>();
        repo.Setup(r => r.GetByIdAsync(model.Id)).ReturnsAsync(model);
        repo.Setup(r => r.UpdateAsync(It.IsAny<DoctorModel>())).ReturnsAsync(true);
        var userRepo = new Mock<IUserRepository>();
        var mapper = CreateMapper();
        var service = new DoctorService(repo.Object, userRepo.Object, mapper);

        var dto = new DoctorDetailDto
        {
            Id = model.Id,
            Gender = Gender.Male,
            Birthday = new DateTime(1990, 2, 2),
            Title = DoctorTitle.Intermediate,
            LicenseNumber = "XYZ",
            Specialty = "S1",
            Status = DoctorStatus.Inactive,
            WorkStatus = DoctorWorkStatus.VisitOutside,
            PinyinCode = "py",
            Remark = "r",
            ContactNumber = "c"
        };

        var result = await service.UpdateAsync(dto);

        Assert.True(result);
        Assert.Equal(dto.Birthday, model.Birthday);
        Assert.Equal(dto.Title, model.Title);
        Assert.Equal(dto.LicenseNumber, model.LicenseNumber);
        Assert.Equal(dto.Specialty, model.Specialty);
        Assert.Equal(dto.Status, model.Status);
        Assert.Equal(dto.WorkStatus, model.WorkStatus);
        var expectedCode2 = model.User.PinyinCode;
        Assert.Equal(expectedCode2, model.PinyinCode);
        Assert.Equal(dto.Remark, model.Remark);
        Assert.Equal(dto.ContactNumber, model.ContactNumber);
        Assert.Equal(dto.Gender, model.Gender);
    }
}
