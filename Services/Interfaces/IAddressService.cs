using Fruitables.Models;

namespace Fruitables.Services.Interfaces;

/// <summary>
/// Service interface for managing user addresses
/// </summary>
public interface IAddressService
{
    /// <summary>
    /// Gets all addresses for a specific user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>List of addresses ordered by default flag and creation date</returns>
    Task<List<Address>> GetUserAddressesAsync(int userId);

    /// <summary>
    /// Gets a specific address by ID
    /// </summary>
    /// <param name="id">The address ID</param>
    /// <returns>Address or null if not found</returns>
    Task<Address?> GetAddressByIdAsync(int id);

    /// <summary>
    /// Gets the default address for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>Default address or null if no default set</returns>
    Task<Address?> GetDefaultAddressAsync(int userId);

    /// <summary>
    /// Creates a new address
    /// </summary>
    /// <param name="address">The address to create</param>
    /// <returns>The created address with ID</returns>
    Task<Address> CreateAddressAsync(Address address);

    /// <summary>
    /// Updates an existing address
    /// </summary>
    /// <param name="address">The address to update</param>
    /// <returns>The updated address</returns>
    Task<Address> UpdateAddressAsync(Address address);

    /// <summary>
    /// Deletes an address
    /// </summary>
    /// <param name="id">The address ID to delete</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAddressAsync(int id);

    /// <summary>
    /// Sets an address as the default for a user
    /// Automatically clears the default flag from other addresses
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="addressId">The address ID to set as default</param>
    /// <returns>True if successful</returns>
    Task<bool> SetDefaultAddressAsync(int userId, int addressId);
}
