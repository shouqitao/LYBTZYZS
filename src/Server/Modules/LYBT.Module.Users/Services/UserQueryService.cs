using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Mapping;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using GenericErrorCode = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Module.Users.Services
{
    public class UserQueryService : IUserQueryService
    {
        private readonly IUserRepository _repository;
        private readonly UserMapper _mapper = new();

        public UserQueryService(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<PagedResult<UserListDto>>> GetPagedAsync(
            int page = 1,
            int pageSize = 20,
            string? keyword = null,
            UserRole? role = null,
            CommonStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            var pagedResult = await _repository.GetPagedAsync(page, pageSize, keyword, role, status, cancellationToken);
            var dtos = _mapper.ToListDtos(pagedResult.Items.ToList());

            var result = new PagedResult<UserListDto>
            {
                Items = dtos,
                TotalCount = pagedResult.TotalCount,
                CurrentPage = page,
                PageSize = pageSize
            };

            return Result<PagedResult<UserListDto>>.Success(result);
        }

        public async Task<Result<UserDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            if (entity == null)
                return Result<UserDetailDto>.Failure(GenericErrorCode.UserNotFound);

            var dto = _mapper.ToDetailDto(entity);
            return Result<UserDetailDto>.Success(dto);
        }

        public async Task<Result<List<UserListDto>>> SearchAsync(string keyword, CancellationToken cancellationToken = default)
        {
            var entities = await _repository.FindAsync(u =>
                u.UserName.Contains(keyword) ||
                u.RealName.Contains(keyword) ||
                (u.Email != null && u.Email.Contains(keyword)), cancellationToken);

            var dtos = _mapper.ToListDtos(entities.ToList());
            return Result<List<UserListDto>>.Success(dtos);
        }
    }
}
