using EcommercePlatform.Application.Common.Interfaces;
using EcommercePlatform.Application.Features.Orders.DTOs;
using EcommercePlatform.Domain.Entities;
using EcommercePlatform.Domain.Enums;
using EcommercePlatform.Domain.Exceptions;
using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EcommercePlatform.Application.Features.Orders.Commands.UpdateOrderStatus;

public sealed class UpdateOrderStatusHandler(
    IRepository<Order> orderRepository,
    IRepository<User> userRepository,
    IRepository<LoyaltyPointHistory> loyaltyPointHistoryRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IRequestHandler<UpdateOrderStatusCommand, OrderDto>
{
    private const decimal AmountPerLoyaltyPoint = 100_000m;

    public async Task<OrderDto> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        if (!UpdateOrderStatusValidator.TryParseOrderStatus(request.Status, out var targetStatus))
        {
            throw new DomainException("Invalid order status.");
        }

        return await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var order = await orderRepository
                .AsQueryable(asNoTracking: false)
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
                ?? throw new NotFoundException(nameof(Order), request.OrderId);

            order.UpdateStatus(targetStatus);

            if (targetStatus == OrderStatus.Completed)
            {
                var pointsEarned = (int)(order.TotalAmount / AmountPerLoyaltyPoint);
                if (pointsEarned > 0)
                {
                    var user = await userRepository
                        .AsQueryable(asNoTracking: false)
                        .FirstOrDefaultAsync(u => u.Id == order.UserId, ct)
                        ?? throw new NotFoundException(nameof(User), order.UserId);

                    user.AddLoyaltyPoints(pointsEarned);

                    var history = new LoyaltyPointHistory(
                        user.Id,
                        order.Id,
                        pointsEarned,
                        $"Earned {pointsEarned} loyalty points from order {order.OrderNumber}.");

                    await loyaltyPointHistoryRepository.AddAsync(history, ct);
                }
            }

            return mapper.Map<OrderDto>(order);
        }, cancellationToken);
    }
}
