using AutoMapper;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Module.Users.Profiles
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<User, UserDto>();
            CreateMap<UserCreateDto, User>();
            CreateMap<UserUpdateDto, User>();
        }
    }
}
