﻿using HandmadeProductManagement.Contract.Repositories.Interface;
using HandmadeProductManagement.Contract.Services.Interface;
using HandmadeProductManagement.Core.Base;
using HandmadeProductManagement.Core.Constants;
using HandmadeProductManagement.ModelViews.UserModelViews;
using HandmadeProductManagement.Repositories.Entity;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using HandmadeProductManagement.ModelViews.NotificationModelViews;
using HandmadeProductManagement.Contract.Repositories.Entity;
using Microsoft.AspNetCore.Http;
using HandmadeProductManagement.Core.Common;
namespace HandmadeProductManagement.Services.Service
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<UpdateUserDTO> _updateValidator;
        private string Url = "https://" + "localhost:44328/";
        public UserService(IUnitOfWork unitOfWork, IValidator<UpdateUserDTO> updateValidator)
        {
            _unitOfWork = unitOfWork;
            _updateValidator = updateValidator;
        }

        public async Task<IList<UserResponseModel>> GetAll()
        {

            var users = await _unitOfWork.GetRepository<ApplicationUser>()
                .Entities
                .Select(user => new UserResponseModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    CreatedBy = user.CreatedBy,
                    LastUpdatedBy = user.LastUpdatedBy,
                    DeletedBy = user.DeletedBy,
                    CreatedTime = user.CreatedTime,
                    LastUpdatedTime = user.LastUpdatedTime,
                    DeletedTime = user.DeletedTime,
                    Status = user.Status,
                })
                .ToListAsync();

            return users;

        }

        public async Task<UserResponseByIdModel> GetById(string id)
        {
            // Ensure the id is a valid Guid
            if (!Guid.TryParse(id, out Guid userId))
            {
                throw new BaseException.BadRequestException(StatusCodeHelper.BadRequest.ToString(), Constants.ErrorMessageInvalidGuidFormat);
            }

            var user = await _unitOfWork.GetRepository<ApplicationUser>()
                .Entities
                .Where(u => u.Id == userId)
                .Select(user => new UserResponseByIdModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    NormalizedUserName = user.NormalizedUserName,
                    Email = user.Email,
                    NormalizedEmail = user.NormalizedEmail,
                    EmailConfirmed = user.EmailConfirmed,
                    PhoneNumber = user.PhoneNumber,
                    PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    LockoutEnd = user.LockoutEnd,
                    LockoutEnabled = user.LockoutEnabled,
                    AccessFailedCount = user.AccessFailedCount,
                })
                .FirstOrDefaultAsync() ?? throw new BaseException.NotFoundException(StatusCodeHelper.NotFound.ToString(), Constants.ErrorMessageUserNotFound);

            return user;
        }

        public async Task<bool> UpdateUser(string id, UpdateUserDTO updateUserDTO)
        {
            if (!Guid.TryParse(id, out Guid userId))
            {
                throw new BaseException.BadRequestException(StatusCodeHelper.BadRequest.ToString(), Constants.ErrorMessageInvalidGuidFormat);
            }

            // Query user
            var user = await _unitOfWork.GetRepository<ApplicationUser>()
                .Entities
                .Where(u => u.Id == userId)
                .FirstOrDefaultAsync() ?? throw new BaseException.NotFoundException(StatusCodeHelper.NotFound.ToString(), Constants.ErrorMessageUserNotFound);

            // Check format DTO by fluent validation
            var updateValidation = _updateValidator.Validate(updateUserDTO);
            if (!updateValidation.IsValid)
            {
                throw new ValidationException(updateValidation.Errors);
            }

            // Check existing unique fields
            if (!string.IsNullOrWhiteSpace(updateUserDTO.UserName))
            {
                var existingUsername = await _unitOfWork.GetRepository<ApplicationUser>().Entities
                    .AnyAsync(u => u.UserName == updateUserDTO.UserName && u.Id != userId);
                if (existingUsername)
                {
                    throw new BaseException.BadRequestException(StatusCodeHelper.BadRequest.ToString(), Constants.ErrorMessageUsernameExists);
                }
                user.UserName = updateUserDTO.UserName;
                user.NormalizedUserName = updateUserDTO.UserName.ToUpper();
            }

            if (!string.IsNullOrWhiteSpace(updateUserDTO.Email))
            {
                var existingUserWithSameEmail = await _unitOfWork.GetRepository<ApplicationUser>()
                    .Entities
                    .AnyAsync(u => u.Email == updateUserDTO.Email && u.Id != userId);
                if (existingUserWithSameEmail)
                {
                    throw new BaseException.BadRequestException(StatusCodeHelper.BadRequest.ToString(), Constants.ErrorMessageEmailExists);
                }
                user.Email = updateUserDTO.Email;
                user.NormalizedEmail = updateUserDTO.Email.ToUpper();
            }

            if (!string.IsNullOrWhiteSpace(updateUserDTO.PhoneNumber))
            {
                var existingUserWithSamePhoneNumber = await _unitOfWork.GetRepository<ApplicationUser>()
                    .Entities
                    .AnyAsync(u => u.PhoneNumber == updateUserDTO.PhoneNumber && u.Id != userId);
                if (existingUserWithSamePhoneNumber)
                {
                    throw new BaseException.BadRequestException(StatusCodeHelper.BadRequest.ToString(), Constants.ErrorMessagePhoneNumberExists);
                }
                user.PhoneNumber = updateUserDTO.PhoneNumber;
            }

            user.TwoFactorEnabled = updateUserDTO.TwoFactorEnabled;
            user.LastUpdatedTime = DateTime.UtcNow;

            await _unitOfWork.GetRepository<ApplicationUser>().UpdateAsync(user);
            await _unitOfWork.SaveAsync();

            return true;
        }

        public async Task<bool> DeleteUser(string Id)
        {
            if (!Guid.TryParse(Id, out Guid userId))
            {
                throw new BaseException.BadRequestException(StatusCodeHelper.BadRequest.ToString(), Constants.ErrorMessageInvalidGuidFormat);
            }

            var user = await _unitOfWork.GetRepository<ApplicationUser>()
               .Entities
               .Where(u => u.Id == userId)
               .FirstOrDefaultAsync();

            if (user == null || user.Status == Constants.UserInactiveStatus)
            {
                throw new BaseException.NotFoundException(StatusCodeHelper.NotFound.ToString(), Constants.ErrorMessageUserNotFound);
            }

            user.Status = Constants.UserInactiveStatus;
            user.DeletedBy = Constants.RoleAdmin;
            user.DeletedTime = DateTime.UtcNow;

            _unitOfWork.GetRepository<ApplicationUser>().Update(user);
            await _unitOfWork.SaveAsync();

            return true;
        }

        public async Task<IList<NotificationModel>> GetNotificationList(string Id)
        {
            if (!Guid.TryParse(Id, out Guid userId))
            {
                throw new BaseException.BadRequestException(StatusCodeHelper.BadRequest.ToString(), Constants.ErrorMessageInvalidGuidFormat);
            }

            var shopIds = await _unitOfWork.GetRepository<Shop>()
                .Entities
                .Where(shop => shop.UserId == userId)
                .Select(shop => shop.Id)
                .ToListAsync() ?? throw new BaseException.NotFoundException(StatusCodeHelper.NotFound.ToString(), Constants.ErrorMessageUserNotFound);

            var reviews = await _unitOfWork.GetRepository<Review>()
                .Entities
                .Where(r => r.UserId == userId && r.Reply == null)
                .Include(r => r.User)
                .ToListAsync();

            var notifications = reviews.Select(review => new NotificationModel
            {
                Id = review.Id,
                Message = $"Sản phẩm của bạn đã được {review.User.UserName} review",
                Tag = Constants.NotificationTagReview,
                URL = $"/api/review/{review.Id}"
            }).ToList();

            return notifications;
        }

        public async Task<IList<NotificationModel>> GetNewOrderNotificationList(string Id)
        {
            if (!Guid.TryParse(Id, out Guid userId))
            {
                throw new BaseException.BadRequestException(StatusCodeHelper.BadRequest.ToString(), Constants.ErrorMessageInvalidGuidFormat);
            }

            var urlRoot = Constants.ApiBaseUrl;
            var fromDate = DateTime.UtcNow.AddDays(-2); // Filter orders from the last 2 days

            // Lấy danh sách đơn hàng trong vòng 2 ngày dựa trên ShopId của người dùng (người bán), sắp xếp theo LastUpdatedTime (tăng dần)
            var orders = await _unitOfWork.GetRepository<Order>()
                .Entities
                .Where(o => o.OrderDetails.Any(od => od.ProductItem.Product.Shop.UserId == userId) && o.OrderDate >= fromDate)
                .OrderBy(o => o.LastUpdatedTime) // Sắp xếp theo LastUpdatedTime tăng dần
                .Include(o => o.User) // Bao gồm thông tin người mua
                .ToListAsync();

            if (!orders.Any())
            {
                return []; // Không có đơn hàng trong khoảng thời gian
            }

            // Tạo danh sách thông báo cho các đơn hàng
            var notifications = orders.Select(order => new NotificationModel
            {
                Id = order.Id,
                Message = $"Bạn có đơn hàng mới từ {order.CustomerName} với trạng thái: {order.Status} vào ngày: {order.LastUpdatedTime.ToString("dd/MM/yyyy")}",
                Tag = "Order",
                URL = urlRoot + $"/api/order/{order.Id}"
            }).ToList();

            return notifications;
        }

        public async Task<IList<NotificationModel>> GetNewReplyNotificationList(string Id)
        {
            if (!Guid.TryParse(Id, out Guid userId))
            {
                throw new BaseException.BadRequestException(StatusCodeHelper.BadRequest.ToString(), Constants.ErrorMessageInvalidGuidFormat);
            }

            // Get the list of reviews for the user
            var reviews = await _unitOfWork.GetRepository<Review>()
                .Entities
                .Where(r => r.UserId == userId)
                .ToListAsync();

            if (reviews == null || !reviews.Any())
            {
                return new List<NotificationModel>();
            }

            var twoDaysAgo = DateTime.UtcNow.AddDays(-2); // Use UTC for consistency

            // Get all new replies for the user's reviews in the last 2 days
            var replies = await _unitOfWork.GetRepository<Reply>()
                .Entities
                .Where(rep => reviews.Select(r => r.Id).Contains(rep.ReviewId) && rep.Date.Value.Date >= twoDaysAgo.Date) // Filter by reply creation date
                .Include(rep => rep.Review) // Include review
                    .ThenInclude(r => r.Product) // Include product
                    .ThenInclude(p => p.Shop)  // Load shop information from product
                    .ThenInclude(s => s.User)  // Load user (shop owner) from shop
                    .ThenInclude(u => u.UserInfo) // Load UserInfo from User
                .ToListAsync();

            if (replies == null || !replies.Any())
            {
                return new List<NotificationModel>();
            }

            // Create notifications for each new reply
            var replyNotifications = replies.Select(reply => new NotificationModel
            {
                Id = reply.Id,
                Message = $"Bạn đã nhận được phản hồi mới cho review sản phẩm {reply.Review.Product.Shop.User.UserInfo.FullName}",
                Tag = Constants.NotificationTagReply,
                URL = Constants.ApiBaseUrl + $"api/reply/{reply.Id}"
            }).ToList();

            return replyNotifications;
        }

        public async Task<IList<NotificationModel>> GetNewReviewNotificationList(string Id)
        {
            if (!Guid.TryParse(Id, out Guid userId))
            {
                throw new BaseException.BadRequestException(StatusCodeHelper.BadRequest.ToString(), Constants.ErrorMessageInvalidGuidFormat);
            }

            var shopID = await _unitOfWork.GetRepository<Shop>()
                .Entities
                .Where(r => r.UserId == userId)
                .Select(r => r.Id)
                .ToListAsync();

            if (shopID == null || !shopID.Any())
            {
                throw new BaseException.NotFoundException(StatusCodeHelper.NotFound.ToString(), Constants.ErrorMessageUserNotFound);
            }

            var twoDaysAgo = DateTime.UtcNow.AddDays(-2); // Use UTC for consistency

            var review = await _unitOfWork.GetRepository<Review>()
                 .Entities
                 .Where(r => r.Reply == null && r.Date.Value.Date >= twoDaysAgo.Date) // Filter reviews without replies in the last two days
                 .Include(r => r.User) // Include user information from review
                    .ThenInclude(u => u.UserInfo) // Include UserInfo from the reviewer's user
                 .ToListAsync();

            var notifications = review.Select(r => new NotificationModel
            {
                Id = r.Id,
                Message = $"Sản phẩm của bạn đã được {r.User.UserInfo.FullName} review", // Get FullName from review user
                Tag = Constants.NotificationTagReview,
                URL = Constants.ApiBaseUrl + $"api/review/{r.Id}"
            }).ToList();

            return notifications;
        }

        public async Task<IList<NotificationModel>> GetNewStatusChangeNotificationList(string Id)
        {
            if (!Guid.TryParse(Id, out Guid userId))
            {
                throw new BaseException.BadRequestException(StatusCodeHelper.BadRequest.ToString(), Constants.ErrorMessageInvalidGuidFormat); // Use constant for invalid GUID format
            }

            // Retrieve the list of orders for the user
            var orders = await _unitOfWork.GetRepository<Order>()
                .Entities
                .Where(o => o.UserId == userId)
                .Select(o => o.Id)
                .ToListAsync();

            if (orders == null || !orders.Any())
            {
                throw new BaseException.NotFoundException(StatusCodeHelper.NotFound.ToString(), Constants.ErrorMessageUserNotFound); // Use constant for user not found
            }

            var twoDaysAgo = DateTime.UtcNow.AddDays(-2); // Use UTC for consistency

            // Get the status changes for the orders
            var statusChanges = await _unitOfWork.GetRepository<StatusChange>()
                .Entities
                .Where(s => orders.Contains(s.OrderId) && s.ChangeTime.Date >= twoDaysAgo)
                .Include(s => s.Order)
                .OrderBy(s => s.ChangeTime) // Order by change time ascending
                .ToListAsync();

            // Define UTC+7 timezone (Vietnam)
            var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById(Constants.TimeZoneSEAsiaStandard);

            // Create response notifications
            var notifications = statusChanges.Select(status => {
                // Convert time from UTC to UTC+7
                var changeTimeInVietnam = TimeZoneInfo.ConvertTimeFromUtc(status.ChangeTime, vietnamTimeZone);

                return new NotificationModel
                {
                    Id = status.Id,
                    Message = $"Đơn hàng của bạn được {status.Status} lúc {changeTimeInVietnam.ToString(Constants.DateTimeFormat)}",
                    Tag = Constants.NotificationTagStatusChange, // Use constant for notification tag
                    URL = Constants.ApiBaseUrl + $"/api/statuschange/order/{status.OrderId}"
                };
            }).ToList();

            return notifications;
        }

        public async Task<IList<NotificationModel>> NotificationForPaymentExpiration(string Id)
        {
            if (!Guid.TryParse(Id, out Guid userId))
            {
                throw new BaseException.BadRequestException(StatusCodeHelper.BadRequest.ToString(), Constants.ErrorMessageInvalidGuidFormat);
            }

            // Lấy danh sách các thanh toán của người dùng thông qua Order và kiểm tra trạng thái chưa hoàn thành
            var payments = await _unitOfWork.GetRepository<Payment>()
                .Entities
                .Where(p => p.Order != null && p.Order.UserId == userId && p.Status != Constants.PaymentStatusPending)
                .Include(p => p.Order) // Bao gồm bảng Order
                .ToListAsync();

            if (payments == null || !payments.Any())
            {
                return new List<NotificationModel>(); // Nếu không có thanh toán nào thì trả về danh sách trống
            }

            var notifications = new List<NotificationModel>();
            var urlroot = Constants.ApiBaseUrl;

            foreach (var payment in payments)
            {
                // Kiểm tra nếu ngày hết hạn cộng thêm 15 ngày lớn hơn ngày hiện tại
                if (payment.ExpirationDate.HasValue && payment.ExpirationDate.Value.AddDays(Constants.PaymentExpirationDays) > DateTime.UtcNow) 
                {
                    // Thêm thông báo cho thanh toán này
                    notifications.Add(new NotificationModel
                    {
                        Id = payment.Id,
                        Message = $"Thanh toán của bạn sẽ hết hạn vào ngày {payment.ExpirationDate.Value.ToString(Constants.DateTimeFormat)}",
                        Tag = Constants.NotificationTagPaymentExpiration,
                        URL = urlroot + $"/api/payment/{payment.Id}"
                    });
                }
            }

            return notifications;
        }

        public async Task<bool> ReverseDeleteUser(string Id)
        {
            if (!Guid.TryParse(Id, out Guid userId))
            {
                throw new BaseException.BadRequestException(StatusCodeHelper.BadRequest.ToString(), Constants.ErrorMessageInvalidGuidFormat);
            }

            var user = await _unitOfWork.GetRepository<ApplicationUser>()
               .Entities
               .Where(u => u.Id == userId)
               .FirstOrDefaultAsync();

            if (user == null || user.Status == Constants.UserActiveStatus)
            {
                throw new BaseException.NotFoundException(StatusCodeHelper.NotFound.ToString(), Constants.ErrorMessageUserNotFoundOrActive);
            }

            user.Status = Constants.UserActiveStatus;
            user.DeletedBy = null;
            user.DeletedTime = null;

            _unitOfWork.GetRepository<ApplicationUser>().Update(user);
            await _unitOfWork.SaveAsync();

            return true;
        }

        public async Task<UpdateUserResponseModel?> UpdateUserProfile(string id, UpdateUserDTO updateUserProfileDTO)
        {
            if (!Guid.TryParse(id, out Guid userId))
            {
                throw new BaseException.BadRequestException(StatusCodeHelper.BadRequest.ToString(), Constants.ErrorMessageInvalidGuidFormat);
            }

            var user = await _unitOfWork.GetRepository<ApplicationUser>()
                .Entities
                .Where(u => u.Id == userId)
                .FirstOrDefaultAsync() ?? throw new BaseException.NotFoundException(StatusCodeHelper.NotFound.ToString(), Constants.ErrorMessageUserNotFound);

            if (updateUserProfileDTO.UserName != null)
            {
                user.UserName = updateUserProfileDTO.UserName;
                user.NormalizedUserName = updateUserProfileDTO.UserName.ToUpper();
            }

            _unitOfWork.GetRepository<ApplicationUser>().Update(user);
            await _unitOfWork.SaveAsync();

            var updatedUserResponse = new UpdateUserResponseModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
            };

            return updatedUserResponse;
        }

        private async Task<string> SaveAvatarFile(IFormFile avatarFile)
        {
            // Define the directory for saving avatars
            var directoryPath = Path.Combine("wwwroot", "images", "avatars");

            // Ensure the directory exists
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var filePath = Path.Combine(directoryPath, avatarFile.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await avatarFile.CopyToAsync(stream);
            }

            return $"{Constants.AvatarBaseUrl}/{avatarFile.FileName}"; // Return the relative path or URL of the image
        }


    }
}