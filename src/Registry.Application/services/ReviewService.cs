using Application.Interfaces;

namespace Application.services
{
    public class ReviewService(IReviewRepository reviewRepository,IRoleService roleService) : IReviewService
    {
        public async Task ReviewChangeEmailAsync()
        {
            await roleService.ReviewFailedRolesOperationsAsync();
            await reviewRepository.ReviewChangeEmailAsync();
        }
    }
}
