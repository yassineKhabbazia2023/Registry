using Application.Interfaces;

namespace Application.services
{
    public class ReviewService(IReviewRepository reviewRepository) : IReviewService
    {
        
        public async Task ReviewChangeEmailAsync()
        {
            await reviewRepository.ReviewChangeEmailAsync();
        }
    }
}
