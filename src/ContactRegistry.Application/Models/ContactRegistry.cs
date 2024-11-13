namespace Application.Models;
public class ContactRegistry
{
    private int _contactFlagStatus;

    public string? ContactCode { get; set; }
    public string? ContactLastName { get; set; }
    public string? ContactFirstName { get; set; }
    public string? ContactTitle { get; set; }
    public string? ContactFullName { get; set; }
    public string? ContactAddress1 { get; set; }
    public string? ContactAddress2 { get; set; }
    public string? ContactAddress3 { get; set; }
    public string? ContactCity { get; set; }
    public string? ContactPostalCode { get; set; }
    public string? ContactCountry { get; set; }
    public string? ContactPhoneLandLine { get; set; }
    public string? ContactPhoneMobileOffice { get; set; }
    public string? ContactDepartment { get; set; }
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
    public required string ContactSourceName { get; set; }
}