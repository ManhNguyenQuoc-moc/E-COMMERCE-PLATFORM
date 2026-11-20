using EcommercePlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EcommercePlatform.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<Product> Products { get; }
    DbSet<Order> Orders { get; }
    DbSet<Account> Accounts { get; }
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
}
