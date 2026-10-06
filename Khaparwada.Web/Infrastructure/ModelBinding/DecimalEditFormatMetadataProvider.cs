using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Khaparwada.Web.Infrastructure.ModelBinding;

/// <summary>
/// ASP.NET Core writes a decimal into a form box with two decimal places ("21.12") unless the property has
/// its own edit format. Saving such a form unchanged then silently cuts the stored value: for a map
/// coordinate that moves the pin by up to a kilometre. This gives every decimal property without its own
/// [DisplayFormat] a format that keeps all its digits, so no form can lose precision.
/// </summary>
public sealed class DecimalEditFormatMetadataProvider : IDisplayMetadataProvider
{
    // "#" digits are only written when needed: 21.5 stays "21.5", 21.123457 stays "21.123457".
    private const string AllDigits = "{0:0.############################}";

    public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
    {
        var type = Nullable.GetUnderlyingType(context.Key.ModelType) ?? context.Key.ModelType;
        if (type == typeof(decimal) && string.IsNullOrEmpty(context.DisplayMetadata.EditFormatString))
        {
            context.DisplayMetadata.EditFormatString = AllDigits;
            context.DisplayMetadata.HasNonDefaultEditFormat = true;
        }
    }
}
