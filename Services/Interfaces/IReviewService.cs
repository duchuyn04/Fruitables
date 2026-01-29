using Fruitables.Models;

namespace Fruitables.Services.Interfaces;

public interface IReviewService
{
    Task<List<Review>> GetReviewsByProductIdAsync(int productId);
    Task<Review> AddReviewAsync(int productId, int userId, int rating, string? comment);
    Task<double> GetAverageRatingAsync(int productId);
}
