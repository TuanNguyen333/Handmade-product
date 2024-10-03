﻿using HandmadeProductManagement.Contract.Repositories.Entity;
using HandmadeProductManagement.Contract.Repositories.Interface;
using HandmadeProductManagement.Contract.Services.Interface;
using HandmadeProductManagement.Core.Base;
using HandmadeProductManagement.ModelViews.OrderDetailModelViews;
using HandmadeProductManagement.ModelViews.OrderModelViews;
using HandmadeProductManagement.ModelViews.StatusChangeModelViews;
using HandmadeProductManagement.Repositories.Entity;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace HandmadeProductManagement.Services.Service
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStatusChangeService _statusChangeService;

        public OrderService(IUnitOfWork unitOfWork, IStatusChangeService statusChangeService)
        {
            _unitOfWork = unitOfWork;
            _statusChangeService = statusChangeService;
        }

        public async Task<bool> CreateOrderAsync(CreateOrderDto createOrder)
        {
            ValidateOrder(createOrder);

            var userRepository = _unitOfWork.GetRepository<ApplicationUser>();
            var userExists = await userRepository.Entities
                .AnyAsync(u => u.Id.ToString() == createOrder.UserId && !u.DeletedTime.HasValue);

            if (!userExists)
            {
                throw new BaseException.NotFoundException("user_not_found", "User not found.");
            }

            var orderRepository = _unitOfWork.GetRepository<Order>();
            var orderDetailRepository = _unitOfWork.GetRepository<OrderDetail>();
            var totalPrice = createOrder.OrderDetails.Sum(detail => detail.UnitPrice * detail.ProductQuantity);

            var order = new Order
            {
                TotalPrice = (decimal)totalPrice,
                OrderDate = DateTime.UtcNow,
                Status = "Pending",
                UserId = Guid.Parse(createOrder.UserId.ToString()),
                Address = createOrder.Address,
                CustomerName = createOrder.CustomerName,
                Phone = createOrder.Phone,
                Note = createOrder.Note,
                CancelReasonId = createOrder.CancelReasonId,
                CreatedBy = createOrder.UserId,
                LastUpdatedBy = createOrder.UserId
            };

            await orderRepository.InsertAsync(order);
            await _unitOfWork.SaveAsync();

            foreach (var detail in createOrder.OrderDetails)
            {
                ValidateOrderDetail(detail);

                var orderDetail = new OrderDetail
                {
                    ProductId = detail.ProductId,
                    ProductQuantity = detail.ProductQuantity,
                    UnitPrice = detail.UnitPrice,
                    OrderId = order.Id,
                    CreatedBy = createOrder.UserId,
                    LastUpdatedBy = createOrder.UserId
                };
                await orderDetailRepository.InsertAsync(orderDetail);
            }

            await _unitOfWork.SaveAsync();

            // Create an initial status change after the order is created
            var statusChangeDto = new StatusChangeForCreationDto
            {
                OrderId = order.Id.ToString(),
                Status = order.Status,
                ChangeTime = DateTime.UtcNow
            };

            await _statusChangeService.Create(statusChangeDto);

            return true;
        }

        public async Task<IList<OrderResponseModel>> GetAllOrdersAsync()
        {
            IQueryable<Order> query = _unitOfWork.GetRepository<Order>().Entities
                .Where(order => !order.DeletedTime.HasValue);
            var result = await query.Select(order => new OrderResponseModel
            {
                Id = order.Id,
                TotalPrice = order.TotalPrice,
                OrderDate = order.OrderDate,
                Status = order.Status,
                UserId = order.UserId,
                Address = order.Address,
                CustomerName = order.CustomerName,
                Phone = order.Phone,
                Note = order.Note,
                CancelReasonId = order.CancelReasonId
            }).ToListAsync();

            return result;
        }

        public async Task<OrderResponseModel> GetOrderByIdAsync(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
            {
                throw new BaseException.BadRequestException("empty_order_id", "Order ID is required.");
            }

            if (!Guid.TryParse(orderId, out _))
            {
                throw new BaseException.BadRequestException("invalid_order_id_format", "Order ID format is invalid. Example: 123e4567-e89b-12d3-a456-426614174000.");
            }

            var repository = _unitOfWork.GetRepository<Order>();
            var order = await repository.Entities
                .FirstOrDefaultAsync(o => o.Id == orderId && !o.DeletedTime.HasValue);

            if (order == null)
            {
                throw new BaseException.NotFoundException("order_not_found", "Order not found.");
            }

            return new OrderResponseModel
            {
                Id = order.Id,
                TotalPrice = order.TotalPrice,
                OrderDate = order.OrderDate,
                Status = order.Status,
                UserId = order.UserId,
                Address = order.Address,
                CustomerName = order.CustomerName,
                Phone = order.Phone,
                Note = order.Note,
                CancelReasonId = order.CancelReasonId
            };
        }

        public async Task<bool> UpdateOrderAsync(string orderId, CreateOrderDto order, string cancelReasonId)
        {
            if (string.IsNullOrWhiteSpace(orderId) || !Guid.TryParse(orderId, out _))
            {
                throw new BaseException.BadRequestException("invalid_order_id", "Order ID is not in the correct format. Ex: 123e4567-e89b-12d3-a456-426614174000.");
            }

            ValidateOrder(order);

            var repository = _unitOfWork.GetRepository<Order>();
            var existingOrder = await repository.Entities
                .FirstOrDefaultAsync(o => o.Id == orderId && !o.DeletedTime.HasValue);

            if (existingOrder == null)
            {
                throw new BaseException.NotFoundException("order_not_found", "Order not found.");
            }

            existingOrder.Address = order.Address;
            existingOrder.CustomerName = order.CustomerName;
            existingOrder.Phone = order.Phone;
            existingOrder.Note = order.Note;
            existingOrder.CancelReasonId = order.CancelReasonId;
            existingOrder.LastUpdatedBy = order.UserId.ToString();
            existingOrder.LastUpdatedTime = DateTime.UtcNow;

            repository.Update(existingOrder);
            await _unitOfWork.SaveAsync();

            return true;
        }

        public async Task<IList<OrderResponseModel>> GetOrderByUserIdAsync(Guid userId)
        {
            var userRepository = _unitOfWork.GetRepository<ApplicationUser>();
            var userExists = await userRepository.Entities
                .AnyAsync(u => u.Id == userId && !u.DeletedTime.HasValue);
            if (!userExists)
            {
                throw new BaseException.NotFoundException("user_not_found", "User not found.");
            }

            var repository = _unitOfWork.GetRepository<Order>();
            var orders = await repository.Entities
                .Where(o => o.UserId == userId && !o.DeletedTime.HasValue)
                .Select(order => new OrderResponseModel
                {
                    Id = order.Id,
                    TotalPrice = order.TotalPrice,
                    OrderDate = order.OrderDate,
                    Status = order.Status,
                    UserId = order.UserId,
                    Address = order.Address,
                    CustomerName = order.CustomerName,
                    Phone = order.Phone,
                    Note = order.Note,
                    CancelReasonId = order.CancelReasonId
                }).ToListAsync();

            return orders;
        }

        public async Task<bool> DeleteOrderAsync(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
            {
                throw new BaseException.BadRequestException("empty_order_id", "Order ID is required.");
            }

            if (!Guid.TryParse(orderId, out _))
            {
                throw new BaseException.BadRequestException("invalid_order_id_format", "Order ID format is invalid. Example: 123e4567-e89b-12d3-a456-426614174000.");
            }

            var repository = _unitOfWork.GetRepository<Order>();
            var order = await repository.Entities
                .FirstOrDefaultAsync(o => o.Id == orderId && !o.DeletedTime.HasValue);
            if (order == null)
            {
                throw new BaseException.NotFoundException("order_not_found", "Order not found.");
            }

            order.DeletedBy = order.UserId.ToString();
            order.DeletedTime = DateTime.UtcNow;

            repository.Update(order);
            await _unitOfWork.SaveAsync();
            return true;
        }

        public async Task<bool> UpdateOrderStatusAsync(string orderId, string status, string cancelReasonId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
            {
                throw new BaseException.BadRequestException("invalid_order_id", "Order ID is required.");
            }

            if (!Guid.TryParse(orderId, out _))
            {
                throw new BaseException.BadRequestException("invalid_order_id_format", "Order ID format is invalid. Example: 123e4567-e89b-12d3-a456-426614174000.");
            }

            if (string.IsNullOrWhiteSpace(status))
            {
                throw new BaseException.BadRequestException("invalid_status", "Status cannot be null or empty.");
            }

            var repository = _unitOfWork.GetRepository<Order>();
            var existingOrder = await repository.Entities
                .FirstOrDefaultAsync(o => o.Id == orderId && !o.DeletedTime.HasValue);

            if (existingOrder == null)
            {
                throw new BaseException.NotFoundException("order_not_found", "Order not found.");
            }

            // Validate Status Flow
            var validStatusTransitions = new Dictionary<string, List<string>>
                {
                    { "Pending", new List<string> { "Canceled", "Awaiting Payment" } },
                    { "Awaiting Payment", new List<string> { "Canceled", "Processing" } },
                    { "Processing", new List<string> { "Delivering" } },
                    { "Delivering", new List<string> { "Shipped", "Delivery Failed" } },
                    { "Delivery Failed", new List<string> { "On Hold" } },
                    { "On Hold", new List<string> { "Delivering Retry", "Refund Requested" } },
                    { "Refund Requested", new List<string> { "Refund Denied", "Refund Approve" } },
                    { "Refund Approve", new List<string> { "Returning" } },
                    { "Returning", new List<string> { "Return Failed", "Returned" } },
                    { "Return Failed", new List<string> { "On Hold" } },
                    { "Returned", new List<string> { "Refunded" } },
                    { "Refunded", new List<string> { "Closed" } },
                    { "Canceled", new List<string> { "Closed" } }
                };

            var allValidStatuses = validStatusTransitions.Keys
                .Concat(validStatusTransitions.Values.SelectMany(v => v))
                .Distinct()
                .ToList();

            if (!allValidStatuses.Contains(status))
            {
                throw new BaseException.BadRequestException("invalid_status", $"Status {status} is not a valid status.");
            }

            if (!validStatusTransitions.ContainsKey(existingOrder.Status) ||
                !validStatusTransitions[existingOrder.Status].Contains(status))
            {
                throw new BaseException.BadRequestException("invalid_status_transition", $"Cannot transition from {existingOrder.Status} to {status}.");
            }

            // Validate if updatedStatus is Canceled
            if (status == "Canceled")
            {
                if (string.IsNullOrWhiteSpace(cancelReasonId))
                {
                    throw new BaseException.BadRequestException("validation_failed", "CancelReasonId is required when updating status to {Canceled}.");
                }

                var cancelReason = await _unitOfWork.GetRepository<CancelReason>().GetByIdAsync(cancelReasonId);

                if (cancelReason == null)
                {
                    throw new BaseException.NotFoundException("not_found", $"Cancel Reason not found. {existingOrder.CancelReasonId}");
                }

                existingOrder.CancelReasonId = cancelReasonId;
            }


            existingOrder.Status = status;
            existingOrder.LastUpdatedBy = existingOrder.UserId.ToString();
            existingOrder.LastUpdatedTime = DateTime.UtcNow;

            // Create a new status change record after updating the order status
            var statusChangeDto = new StatusChangeForCreationDto
            {
                OrderId = orderId,
                Status = status,
                ChangeTime = DateTime.UtcNow
            };

            repository.Update(existingOrder);
            await _statusChangeService.Create(statusChangeDto);
            await _unitOfWork.SaveAsync();

            return true;
        }

        private void ValidateOrder(CreateOrderDto order)
        {
            if (string.IsNullOrWhiteSpace(order.UserId))
            {
                throw new BaseException.BadRequestException("invalid_user_id", "Please input User id.");
            }

            if (!Guid.TryParse(order.UserId, out _))
            {
                throw new BaseException.BadRequestException("invalid_user_id_format", "User ID format is invalid. Example: 123e4567-e89b-12d3-a456-426614174000.");
            }

            if (string.IsNullOrWhiteSpace(order.Address))
            {
                throw new BaseException.BadRequestException("invalid_address", "Address cannot be null or empty.");
            }

            if (Regex.IsMatch(order.Address, @"[^a-zA-Z0-9\s]"))
            {
                throw new BaseException.BadRequestException("invalid_address_format", "Address cannot contain special characters.");
            }

            if (string.IsNullOrWhiteSpace(order.CustomerName))
            {
                throw new BaseException.BadRequestException("invalid_customer_name", "Customer name cannot be null or empty.");
            }

            if (Regex.IsMatch(order.CustomerName, @"[^a-zA-Z\s]"))
            {
                throw new BaseException.BadRequestException("invalid_customer_name_format", "Customer name can only contain letters and spaces.");
            }

            if (string.IsNullOrWhiteSpace(order.Phone))
            {
                throw new BaseException.BadRequestException("invalid_phone", "Phone number cannot be null or empty.");
            }

            if (!Regex.IsMatch(order.Phone, @"^\d{1,10}$"))
            {
                throw new BaseException.BadRequestException("invalid_phone_format", "Phone number must be numeric and up to 10 digits.");
            }
        }

        private void ValidateOrderDetail(OrderDetailForCreationDto detail)
        {
            if (string.IsNullOrWhiteSpace(detail.ProductId))
            {
                throw new BaseException.BadRequestException("invalid_product_id", "Please input Product id.");
            }

            if (!Guid.TryParse(detail.ProductId, out _))
            {
                throw new BaseException.BadRequestException("invalid_product_id_format", "Product ID format is invalid. Example: 123e4567-e89b-12d3-a456-426614174000.");
            }

            if (string.IsNullOrWhiteSpace(detail.OrderId))
            {
                throw new BaseException.BadRequestException("invalid_order_id", "Please input Order id.");
            }

            if (!Guid.TryParse(detail.OrderId, out _))
            {
                throw new BaseException.BadRequestException("invalid_order_id_format", "Order ID format is invalid. Example: 123e4567-e89b-12d3-a456-426614174000.");
            }

            if (detail.ProductQuantity <= 0)
            {
                throw new BaseException.BadRequestException("invalid_product_quantity", "Product quantity must be greater than zero and not null.");
            }

            if (detail.UnitPrice <= 0)
            {
                throw new BaseException.BadRequestException("invalid_unit_price", "Unit price must be greater than zero and not null.");
            }
        }

    }
}