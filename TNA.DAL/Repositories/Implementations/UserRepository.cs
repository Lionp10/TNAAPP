using Microsoft.EntityFrameworkCore;
using TNA.DAL.DbContext;
using TNA.DAL.Entities;
using TNA.DAL.Repositories.Interfaces;

namespace TNA.DAL.Repositories.Implementations
{
    public class UserRepository : IUserRepository
    {
        private readonly TNADbContext _db;

        public UserRepository(TNADbContext db)
        {
            _db = db;
        }

        public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        }

        public async Task<User?> GetByNicknameAsync(string nickname, CancellationToken cancellationToken = default)
        {
            return await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Nickname == nickname, cancellationToken);
        }

        public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Users
                .AsNoTracking()
                .OrderBy(u => u.Nickname)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<User>> GetByRoleIdAsync(int roleId, CancellationToken cancellationToken = default)
        {
            return await _db.Users
                .AsNoTracking()
                .Where(u => u.RoleId == roleId)
                .OrderBy(u => u.Nickname)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CreateAsync(User user, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(user.Email)) throw new ArgumentException("Email is required.", nameof(user));
            if (string.IsNullOrWhiteSpace(user.Nickname)) throw new ArgumentException("Nickname is required.", nameof(user));
            if (string.IsNullOrWhiteSpace(user.PasswordHash)) throw new ArgumentException("PasswordHash is required.", nameof(user));

            // Comprueba unicidad mínima (evita excepción por índice único)
            if (await EmailExistsAsync(user.Email, cancellationToken))
                throw new InvalidOperationException("El email ya está registrado.");

            if (await NicknameExistsAsync(user.Nickname, cancellationToken))
                throw new InvalidOperationException("El nickname ya está en uso.");

            user.CreatedAt ??= DateTime.Now;
            // Por defecto habilitado si no se especificó
            user.Enabled = user.Enabled;

            await _db.Users.AddAsync(user, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            return user.Id;
        }

        public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            var existing = await _db.Users.FirstOrDefaultAsync(u => u.Id == user.Id, cancellationToken);
            if (existing is null) throw new KeyNotFoundException($"User with Id {user.Id} not found.");

            // Si cambia email o nickname, comprobar unicidad
            if (!string.Equals(existing.Email, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                if (await EmailExistsAsync(user.Email!, cancellationToken))
                    throw new InvalidOperationException("El email ya está registrado por otro usuario.");
                existing.Email = user.Email!;
            }

            if (!string.Equals(existing.Nickname, user.Nickname, StringComparison.OrdinalIgnoreCase))
            {
                if (await NicknameExistsAsync(user.Nickname!, cancellationToken))
                    throw new InvalidOperationException("El nickname ya está en uso por otro usuario.");
                existing.Nickname = user.Nickname!;
            }

            // Actualiza campos permitidos
            existing.PasswordHash = user.PasswordHash;
            existing.RoleId = user.RoleId;
            existing.MemberId = user.MemberId;
            existing.Enabled = user.Enabled;
            // No sobrescribimos CreatedAt por seguridad

            _db.Users.Update(existing);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var existing = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
            if (existing is null) return;

            existing.Enabled = false;
            _db.Users.Update(existing);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task HardDeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var existing = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
            if (existing is null) return;

            _db.Users.Remove(existing);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return await _db.Users.AnyAsync(u => u.Email == email, cancellationToken);
        }

        public async Task<bool> NicknameExistsAsync(string nickname, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(nickname)) return false;
            return await _db.Users.AnyAsync(u => u.Nickname == nickname, cancellationToken);
        }
    }
}
