using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.services
{
    public class ReviewService(IReviewRepository reviewRepository, IRoleService roleService, ILogger<ReviewService> logger) : IReviewService
    {
        public async Task ReviewChangeEmailAsync()
        {
            try
            {
                logger.LogInformation("Review change email started at: {Date} - ReviewChangeEmailAsync", DateTime.UtcNow);
                await roleService.ReviewFailedRolesOperationsAsync();
                await reviewRepository.ReviewChangeEmailAsync();
                logger.LogInformation("Review change email finished at: {Date} - ReviewChangeEmailAsync", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Review change email failed at: {Date} - ReviewChangeEmailAsync", DateTime.UtcNow);
                throw new InvalidOperationException("Failed to review change email operations", ex);
            }
        }
    }
}
