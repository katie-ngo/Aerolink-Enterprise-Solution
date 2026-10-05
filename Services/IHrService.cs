using Aerolink_Enterprise_Solution.Models;

namespace Aerolink_Enterprise_Solution.Services
{
    public interface IHrService
    {
        Task<(bool IsSuccess, EmployeeDetailDto? Employee, string ErrorMessage)> ValidateStaffLoginAsync(string employeeId, string email);
    }
}
