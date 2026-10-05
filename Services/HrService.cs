using System.Net;
using System.Net.Http.Json;
using Aerolink_Enterprise_Solution.Models;


namespace Aerolink_Enterprise_Solution.Services;

public class HrService : IHrService
{
    private readonly HttpClient _httpClient;

    public HrService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(bool IsSuccess, EmployeeDetailDto? Employee, string ErrorMessage)> ValidateStaffLoginAsync(string employeeIdInput, string emailInput)
    {
        if (!int.TryParse(employeeIdInput, out int employeeId))
        {
            return (false, null, "Employee ID must be a valid number.");
        }

        try
        {
            var response = await _httpClient.GetAsync($"api/employees/{employeeId}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return (false, null, $"Employee with ID {employeeId} was not found in the HR system.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return (false, null, $"HR System error: HTTP {response.StatusCode}");
            }

            var employee = await response.Content.ReadFromJsonAsync<EmployeeDetailDto>();
            if (employee == null)
            {
                return (false, null, "Could not process employee data from HR System.");
            }

            if (emailInput != employee.Email)
            {
                return (false, null, "The entered email address does not match this Employee ID.");
            }

            var allowedRoles = new[] { "Baggage Handler", "BG_Supervisor"};
            if (employee.JobTitle == null || !allowedRoles.Contains(employee.JobTitle))
            {
                return (false, null, $"Access denied. Job Title '{employee.JobTitle}' is not authorized for Baggage Operations.");
            }

            return (true, employee, string.Empty);
        }
        catch (HttpRequestException)
        {
            return (false, null, "Unable to reach the HR System API. Please verify it is running on https://localhost:5001.");
        }
    }
}