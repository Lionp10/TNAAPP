using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TNA.BLL.DTOs;
using TNA.BLL.Services.Interfaces;
using TNA.DAL.Entities;
using TNA.DAL.Repositories.Interfaces;
using System.Threading;

namespace TNA.BLL.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public UserService(IUserRepository repository, IMapper mapper, ILogger<UserService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<UserDTO?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (entity is null) return null;
            return _mapper.Map<UserDTO>(entity);
        }

        public async Task<UserDTO?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            var entity = await _repository.GetByEmailAsync(email, cancellationToken).ConfigureAwait(false);
            return entity is null ? null : _mapper.Map<UserDTO>(entity);
        }

        public async Task<UserDTO?> GetByNicknameAsync(string nickname, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(nickname)) return null;
            var entity = await _repository.GetByNicknameAsync(nickname, cancellationToken).ConfigureAwait(false);
            return entity is null ? null : _mapper.Map<UserDTO>(entity);
        }

        public async Task<List<UserDTO>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var list = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
            return list?.Select(u => _mapper.Map<UserDTO>(u)).ToList() ?? new List<UserDTO>();
        }

        public async Task<List<UserDTO>> GetByRoleIdAsync(int roleId, CancellationToken cancellationToken = default)
        {
            var list = await _repository.GetByRoleIdAsync(roleId, cancellationToken).ConfigureAwait(false);
            return list?.Select(u => _mapper.Map<UserDTO>(u)).ToList() ?? new List<UserDTO>();
        }

        public async Task<int> CreateAsync(UserCreateDTO dto, CancellationToken cancellationToken = default)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            if (string.IsNullOrWhiteSpace(dto.Email)) throw new ArgumentException("Email is required.", nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.Nickname)) throw new ArgumentException("Nickname is required.", nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.Password)) throw new ArgumentException("Password is required.", nameof(dto));

            if (await _repository.EmailExistsAsync(dto.Email, cancellationToken).ConfigureAwait(false))
                throw new InvalidOperationException("El email ya está registrado.");

            if (await _repository.NicknameExistsAsync(dto.Nickname, cancellationToken).ConfigureAwait(false))
                throw new InvalidOperationException("El nickname ya está en uso.");

            var entity = _mapper.Map<User>(dto);
            entity.CreatedAt ??= DateTime.UtcNow;

            entity.PasswordHash = _passwordHasher.HashPassword(entity, dto.Password);

            var id = await _repository.CreateAsync(entity, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("User created with Id {UserId}", id);
            return id;
        }

        public async Task UpdateAsync(UserUpdateDTO dto, CancellationToken cancellationToken = default)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            var entity = _mapper.Map<User>(dto);

            if (string.IsNullOrWhiteSpace(dto.Password))
            {
                var existing = await _repository.GetByIdAsync(dto.Id, cancellationToken).ConfigureAwait(false);
                if (existing is null) throw new KeyNotFoundException($"User with Id {dto.Id} not found.");
                entity.PasswordHash = existing.PasswordHash;
                entity.CreatedAt = existing.CreatedAt;
            }
            else
            {
                entity.PasswordHash = _passwordHasher.HashPassword(entity, dto.Password);
            }

            await _repository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("User updated Id {UserId}", dto.Id);
        }

        public async Task SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            await _repository.SoftDeleteAsync(id, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("User soft-deleted Id {UserId}", id);
        }

        public async Task HardDeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            await _repository.HardDeleteAsync(id, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("User hard-deleted Id {UserId}", id);
        }

        public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _repository.EmailExistsAsync(email, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> NicknameExistsAsync(string nickname, CancellationToken cancellationToken = default)
        {
            return await _repository.NicknameExistsAsync(nickname, cancellationToken).ConfigureAwait(false);
        }
    }
}
