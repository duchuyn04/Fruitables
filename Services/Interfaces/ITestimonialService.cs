using Fruitables.Models;

namespace Fruitables.Services.Interfaces;

public interface ITestimonialService
{
    Task<List<Testimonial>> GetActiveTestimonialsAsync();
    Task<Testimonial> AddTestimonialAsync(Testimonial testimonial);
}
