using Microsoft.EntityFrameworkCore;
using Fruitables.Models;
using Fruitables.Repositories.Interfaces;
using Fruitables.Services.Interfaces;

namespace Fruitables.Services;

/// <summary>
/// Service for managing user addresses
/// </summary>
public class AddressService : IAddressService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IVietnamAddressService _vietnamAddressService;

    public AddressService(IUnitOfWork unitOfWork, IVietnamAddressService vietnamAddressService)
    {
        _unitOfWork = unitOfWork;
        _vietnamAddressService = vietnamAddressService;
    }

    /// <summary>
    /// Gets all addresses for a specific user, ordered by default flag and creation date
    /// </summary>
    public async Task<List<Address>> GetUserAddressesAsync(int userId)
    {
        return await _unitOfWork.Addresses.Query()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Gets a specific address by ID
    /// </summary>
    public async Task<Address?> GetAddressByIdAsync(int id)
    {
        return await _unitOfWork.Addresses.GetByIdAsync(id);
    }

    /// <summary>
    /// Gets the default address for a user
    /// </summary>
    public async Task<Address?> GetDefaultAddressAsync(int userId)
    {
        return await _unitOfWork.Addresses.Query()
            .FirstOrDefaultAsync(a => a.UserId == userId && a.IsDefault);
    }

    /// <summary>
    /// Creates a new address
    /// </summary>
    public async Task<Address> CreateAddressAsync(Address address)
    {
        // Trim all string fields
        address.FullName = address.FullName?.Trim() ?? string.Empty;
        address.Phone = address.Phone?.Trim() ?? string.Empty;
        address.ProvinceName = address.ProvinceName?.Trim() ?? string.Empty;
        address.DistrictName = address.DistrictName?.Trim() ?? string.Empty;
        address.WardName = address.WardName?.Trim() ?? string.Empty;
        address.StreetAddress = address.StreetAddress?.Trim() ?? string.Empty;
        
        // Sanitize StreetAddress to prevent XSS
        address.StreetAddress = _vietnamAddressService.SanitizeStreetAddress(address.StreetAddress);

        // Validate required fields
        if (string.IsNullOrWhiteSpace(address.FullName))
            throw new ArgumentException("FullName is required", nameof(address));
        if (address.FullName.Length > 200)
            throw new ArgumentException("FullName cannot exceed 200 characters", nameof(address));

        if (string.IsNullOrWhiteSpace(address.Phone))
            throw new ArgumentException("Phone is required", nameof(address));
        if (address.Phone.Length > 20)
            throw new ArgumentException("Phone cannot exceed 20 characters", nameof(address));

        if (string.IsNullOrWhiteSpace(address.StreetAddress))
            throw new ArgumentException("StreetAddress is required", nameof(address));
        if (address.StreetAddress.Length > 200)
            throw new ArgumentException("StreetAddress cannot exceed 200 characters", nameof(address));

        if (address.ProvinceCode <= 0)
            throw new ArgumentException("ProvinceCode is required", nameof(address));
        if (address.DistrictCode <= 0)
            throw new ArgumentException("DistrictCode is required", nameof(address));
        if (address.WardCode <= 0)
            throw new ArgumentException("WardCode is required", nameof(address));

        // Set creation time
        address.CreatedAt = DateTime.UtcNow;

        // If this is the first address for the user, make it default
        if (address.UserId.HasValue)
        {
            var existingAddresses = await GetUserAddressesAsync(address.UserId.Value);
            if (!existingAddresses.Any())
            {
                address.IsDefault = true;
            }
        }

        await _unitOfWork.Addresses.AddAsync(address);
        await _unitOfWork.SaveChangesAsync();

        return address;
    }

    /// <summary>
    /// Updates an existing address
    /// </summary>
    public async Task<Address> UpdateAddressAsync(Address address)
    {
        var existing = await _unitOfWork.Addresses.GetByIdAsync(address.Id);
        if (existing == null)
            throw new InvalidOperationException($"Address with ID {address.Id} not found");

        // Update fields with sanitization for StreetAddress
        existing.FullName = address.FullName;
        existing.Phone = address.Phone;
        existing.ProvinceCode = address.ProvinceCode;
        existing.ProvinceName = address.ProvinceName;
        existing.DistrictCode = address.DistrictCode;
        existing.DistrictName = address.DistrictName;
        existing.WardCode = address.WardCode;
        existing.WardName = address.WardName;
        existing.StreetAddress = _vietnamAddressService.SanitizeStreetAddress(address.StreetAddress);
        existing.IsDefault = address.IsDefault;
        existing.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        return existing;
    }

    /// <summary>
    /// Deletes an address
    /// </summary>
    public async Task<bool> DeleteAddressAsync(int id)
    {
        var address = await _unitOfWork.Addresses.GetByIdAsync(id);
        if (address == null)
            return false;

        // If this was the default address, set another one as default
        if (address.IsDefault && address.UserId.HasValue)
        {
            var otherAddresses = await _unitOfWork.Addresses.Query()
                .Where(a => a.UserId == address.UserId && a.Id != id)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            if (otherAddresses.Any())
            {
                otherAddresses.First().IsDefault = true;
            }
        }

        _unitOfWork.Addresses.Remove(address);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Sets an address as the default for a user
    /// Automatically clears the default flag from other addresses
    /// </summary>
    public async Task<bool> SetDefaultAddressAsync(int userId, int addressId)
    {
        // Get the address to set as default
        var targetAddress = await _unitOfWork.Addresses.GetByIdAsync(addressId);
        if (targetAddress == null || targetAddress.UserId != userId)
            return false;

        // Clear default flag from all other addresses for this user
        var userAddresses = await _unitOfWork.Addresses.Query()
            .Where(a => a.UserId == userId)
            .ToListAsync();

        foreach (var addr in userAddresses)
        {
            addr.IsDefault = (addr.Id == addressId);
        }

        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}
