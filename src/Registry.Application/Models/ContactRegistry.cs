namespace Application.Models;
public class ContactRegistry
{
    private int _contactFlagStatus;

    public string? ContactCode { get; set; } = string.Empty;
    public string? ContactLastName { get; set; } = string.Empty;
    public string? ContactFirstName { get; set; } = string.Empty;
    public string? ContactTitle { get; set; } = string.Empty;
    public string? ContactFullName { get; set; } = string.Empty;
    public string? ContactAddress1 { get; set; } = string.Empty;
    public string? ContactAddress2 { get; set; } = string.Empty;
    public string? ContactAddress3 { get; set; } = string.Empty;
    public string? ContactCity { get; set; } = string.Empty;
    public string? ContactPostalCode { get; set; } = string.Empty;
    public string? ContactCountry { get; set; } = string.Empty;
    public string? ContactPhoneLandLine { get; set; } = string.Empty;
    public string? ContactPhoneMobileOffice { get; set; } = string.Empty;
    public string? ContactDepartment { get; set; } = string.Empty;
    public required string ContactEmailOffice { get; set; }
    public required string ContactFunctionDescription { get; set; }
    public required int ContactFlagStatus
    {
        get { return _contactFlagStatus; }
        set
        {
            if (value == 0 || value == 1)
            {
                _contactFlagStatus = value;
            }
            else
            {
                throw new ArgumentException("ContactFlagStatus must be either 0 or 1.");
            }
        }
    }
    public required string ContactSourceName { get; set; } = string.Empty;
}