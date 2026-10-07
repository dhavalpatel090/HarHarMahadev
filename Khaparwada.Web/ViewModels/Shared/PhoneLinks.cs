namespace Khaparwada.Web.ViewModels.Shared;

/// <summary>Phone links, made the same way everywhere (Home, the Contact page, a place's page).</summary>
public static class PhoneLinks
{
    /// <summary>
    /// "tel:" link: a leading + and the digits only. Spaces and brackets are not allowed in a tel: link, so
    /// "+91 (2634) 123-456" becomes "tel:+912634123456" while the page still shows the number as typed.
    /// </summary>
    public static string? Tel(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var number = phone.Trim();
        return "tel:" + (number.StartsWith('+') ? "+" : string.Empty) + new string(number.Where(char.IsAsciiDigit).ToArray());
    }
}
