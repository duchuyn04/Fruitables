using Microsoft.EntityFrameworkCore;
using Fruitables.Models;
using Fruitables.Repositories.Interfaces;
using Fruitables.Services.Interfaces;

namespace Fruitables.Services;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;

    public ReviewService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<Review>> GetReviewsByProductIdAsync(int productId)
    {
        return await _unitOfWork.Reviews.Query()
            .Where(r => r.ProductId == productId)
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Review> AddReviewAsync(int productId, int userId, int rating, string? comment)
    {
        var review = new Review
        {
            ProductId = productId,
            UserId = userId,
            Rating = rating,
            Comment = comment
        };

        await _unitOfWork.Reviews.AddAsync(review);
        await _unitOfWork.SaveChangesAsync();

        return review;
    }

    public async Task<double> GetAverageRatingAsync(int productId)
    {
        var reviews = await _unitOfWork.Reviews.Query()
            .Where(r => r.ProductId == productId)
            .ToListAsync();

        if (!reviews.Any()) return 0;

        return reviews.Average(r => r.Rating);
    }
}
