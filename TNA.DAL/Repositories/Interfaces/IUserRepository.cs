using TNA.DAL.Entities;

namespace TNA.DAL.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<User?> GetByNicknameAsync(string nickname, CancellationToken cancellationToken = default);
        Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<User>> GetByRoleIdAsync(int roleId, CancellationToken cancellationToken = default);
        Task<int> CreateAsync(User user, CancellationToken cancellationToken = default);
        Task UpdateAsync(User user, CancellationToken cancellationToken = default);
        Task SoftDeleteAsync(int id, CancellationToken cancellationToken = default);
        Task HardDeleteAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
        Task<bool> NicknameExistsAsync(string nickname, CancellationToken cancellationToken = default);
    }
}
