using Fruitables.Models;

namespace Fruitables.ViewModels;

public class HomeViewModel
{
    public List<Category> Categories { get; set; } = new();
    public List<Product> FeaturedProducts { get; set; } = new();
    public List<Product> BestSellerProducts { get; set; } = new();
    public List<Testimonial> Testimonials { get; set; } = new();
}
