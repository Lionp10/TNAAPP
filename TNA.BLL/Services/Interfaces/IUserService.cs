using TNA.BLL.DTOs;

namespace TNA.BLL.Services.Interfaces
{
    public interface IUserService
    {
        Task<UserDTO?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<UserDTO?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<UserDTO?> GetByNicknameAsync(string nickname, CancellationToken cancellationToken = default);
        Task<List<UserDTO>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<List<UserDTO>> GetByRoleIdAsync(int roleId, CancellationToken cancellationToken = default);

        Task<int> CreateAsync(UserCreateDTO dto, CancellationToken cancellationToken = default);
        Task UpdateAsync(UserUpdateDTO dto, CancellationToken cancellationToken = default);
        Task SoftDeleteAsync(int id, CancellationToken cancellationToken = default);
        Task HardDeleteAsync(int id, CancellationToken cancellationToken = default);

        Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
        Task<bool> NicknameExistsAsync(string nickname, CancellationToken cancellationToken = default);
    }
}
