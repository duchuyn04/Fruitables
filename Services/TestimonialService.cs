using Microsoft.EntityFrameworkCore;
using Fruitables.Models;
using Fruitables.Repositories.Interfaces;
using Fruitables.Services.Interfaces;

namespace Fruitables.Services;

public class TestimonialService : ITestimonialService
{
    private readonly IUnitOfWork _unitOfWork;

    public TestimonialService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<Testimonial>> GetActiveTestimonialsAsync()
    {
        return await _unitOfWork.Testimonials.Query()
            .Where(t => t.IsActive)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<Testimonial> AddTestimonialAsync(Testimonial testimonial)
    {
        await _unitOfWork.Testimonials.AddAsync(testimonial);
        await _unitOfWork.SaveChangesAsync();
        return testimonial;
    }
}
