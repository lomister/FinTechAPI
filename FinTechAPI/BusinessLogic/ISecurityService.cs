using FinTechAPI.Models;

namespace FinTechAPI.Services;

public interface ISecurityService
{
    IEnumerable<Transaction> DetectAnomalies(decimal threshold);

    Task<IEnumerable<Transaction>> DetectAnomaliesAsync(decimal threshold);
}