using A1arErpSfabGl07Gateway.Api.Models.Settings;

namespace A1arErpSfabGl07Gateway.Api.Repositories;

/// <summary>
/// Repository interface for GL07 Report Setup operations.
/// </summary>
public interface IGl07ReportSetupRepository
{
    /// <summary>
    /// Gets all GL07 report setups.
    /// </summary>
    Task<IEnumerable<Gl07ReportSetup>> GetAllAsync();

    /// <summary>
    /// Gets all active GL07 report setups.
    /// </summary>
    Task<IEnumerable<Gl07ReportSetup>> GetAllActiveAsync();

    /// <summary>
    /// Gets a GL07 report setup by ID.
    /// </summary>
    Task<Gl07ReportSetup?> GetByIdAsync(int id);

    /// <summary>
    /// Gets a GL07 report setup by ID with its parameters.
    /// </summary>
    Task<Gl07ReportSetup?> GetByIdWithParametersAsync(int id);

    /// <summary>
    /// Gets a GL07 report setup by setup code.
    /// </summary>
    Task<Gl07ReportSetup?> GetBySetupCodeAsync(string setupCode);

    /// <summary>
    /// Creates a new GL07 report setup.
    /// </summary>
    Task<int> CreateAsync(Gl07ReportSetup setup);

    /// <summary>
    /// Updates an existing GL07 report setup.
    /// </summary>
    Task<bool> UpdateAsync(Gl07ReportSetup setup);

    /// <summary>
    /// Deletes a GL07 report setup.
    /// </summary>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Checks if a setup code is unique.
    /// </summary>
    Task<bool> IsSetupCodeUniqueAsync(string setupCode, int? excludeId = null);

    /// <summary>
    /// Adds a parameter to a GL07 report setup.
    /// </summary>
    Task<int> AddParameterAsync(int setupId, Gl07ReportSetupParameter parameter);

    /// <summary>
    /// Updates a parameter.
    /// </summary>
    Task<bool> UpdateParameterAsync(Gl07ReportSetupParameter parameter);

    /// <summary>
    /// Deletes a parameter.
    /// </summary>
    Task<bool> DeleteParameterAsync(int parameterId);

    /// <summary>
    /// Gets parameters for a setup.
    /// </summary>
    Task<IEnumerable<Gl07ReportSetupParameter>> GetParametersAsync(int setupId);
}
