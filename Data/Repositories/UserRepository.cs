using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlowerShop;
using Microsoft.EntityFrameworkCore;
using AdoreFlowerShop.Data;
using AdoreFlowerShop.Data.EntityFramework;
using AdoreFlowerShop.Services;

namespace AdoreFlowerShop.Data.Repositories
{
    internal sealed class UserRepository : IUserRepository
    {
        private readonly FlowerShopDbContext _context;

        public UserRepository(FlowerShopDbContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetAllAsync(CancellationToken ct = default)
        {
            var rows = await _context.Users.AsNoTracking()
                .OrderBy(u => u.Id)
                .ToListAsync(ct);
            return rows.Select(EntityToModelMapper.ToUser).ToList();
        }

        public async Task UpdatePasswordAsync(string userName, string newPassword, CancellationToken ct = default)
        {
            var pwdErr = InputValidator.ValidatePassword(newPassword);
            if (pwdErr != null)
                throw new InvalidOperationException(pwdErr);

            var u = await _context.Users.FirstOrDefaultAsync(x => x.Name == userName, ct);
            if (u == null)
                return;
            u.Password = newPassword;
            await _context.SaveChangesAsync(ct);
        }

        public async Task<string?> TryInsertClientAsync(string name, string password, string phone, CancellationToken ct = default)
        {
            var validationError = InputValidator.ValidateRegistration(name, password, password, phone);
            if (validationError != null)
                return validationError;

            var trimmed = name.Trim();

            var exists = await _context.Users.AnyAsync(x => x.Name == trimmed, ct);
            if (exists)
                return "Это имя пользователя уже занято.";

            var phoneNormalized = InputValidator.NormalizePhoneDigits(phone);
            var nextId = await TableIdAllocator.NextUserIdAsync(_context, ct);

            try
            {
                await TableIdAllocator.InsertWithExplicitIdAsync(
                    _context,
                    "Users",
                    async () =>
                    {
                        _context.Users.Add(new ShopUser
                        {
                            Id = nextId,
                            Name = trimmed,
                            Password = password,
                            Role = "Client",
                            Phone = phoneNormalized
                        });
                        await _context.SaveChangesAsync(ct);
                    },
                    ct: ct);
            }
            catch (Exception ex)
            {
                return ex.InnerException?.Message ?? ex.Message;
            }

            return null;
        }
    }
}
