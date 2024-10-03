﻿using HandmadeProductManagement.ModelViews.OrderModelViews;

namespace HandmadeProductManagement.Contract.Services.Interface
{
    public interface IOrderService
    {
        Task<IList<OrderResponseModel>> GetAllOrdersAsync();
        Task<OrderResponseModel> GetOrderByIdAsync(string orderId);
        Task<bool> CreateOrderAsync(CreateOrderDto createOrder);
        Task<bool> UpdateOrderAsync(string orderId, CreateOrderDto order, string cancelReasonId);
        Task<bool> UpdateOrderStatusAsync(string orderId, string status, string cancelReasonId);
        Task<bool> DeleteOrderAsync(string orderId);
        Task<IList<OrderResponseModel>> GetOrderByUserIdAsync(Guid userId);
    }
}
