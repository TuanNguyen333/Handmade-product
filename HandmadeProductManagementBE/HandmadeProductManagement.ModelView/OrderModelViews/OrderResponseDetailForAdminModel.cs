﻿using HandmadeProductManagement.ModelViews.OrderDetailModelViews;

namespace HandmadeProductManagement.ModelViews.OrderModelViews
{
    public class OrderResponseDetailForAdminModel : OrderResponseModel
    {
        public List<OrderDetailResponseModel> OrderDetails { get; set; } = new List<OrderDetailResponseModel>();
    }
}
