using Microsoft.Playwright;
using Serilog;
using test.playwright.framework.base_abstract;
using test.playwright.framework.utils;

namespace test.playwright.framework.pages.app.components;

public sealed class FieldValidator(IPage page) : BaseProjectElements(page)
{
    private static ILocator FieldRoot(ILocator container) =>
        container.Locator(".v-input, .v-field, [role='combobox'], .text-error").First;

    public static async Task<bool> IsFieldInvalidAsync(ILocator item, bool allowMissingOrHidden = false)
    {
        var root = FieldRoot(item);
        var count = await root.CountAsync();

        if (count == 0)
        {
            if (!allowMissingOrHidden)
            {
                throw new InvalidOperationException("FieldValidator: field root not found for locator. " +
                                                    "Either the locator is wrong or the field is not rendered in this context.");
            }

            Log.Information("FieldValidator: field root not found; treating as not invalid.");
            return false;
        }

        if (!allowMissingOrHidden)
        {
            await root.ExpectSingleVisibleAsync();
        }
        else
        {
            if (!await root.IsVisibleAsync())
            {
                Log.Information("FieldValidator: field root found but not visible; treating as not invalid.");
                return false;
            }
        }

        var cls = await root.GetAttributeAsync("class") ?? string.Empty;
        if (cls.Contains("v-input--error", StringComparison.OrdinalIgnoreCase) ||
            cls.Contains("v-field--error", StringComparison.OrdinalIgnoreCase) ||
            cls.Contains("text-error", StringComparison.OrdinalIgnoreCase))
            return true;

        var ariaInvalid = await root.GetAttributeAsync("aria-invalid");
        if (string.Equals(ariaInvalid, "true", StringComparison.OrdinalIgnoreCase)) return true;

        return await root.Locator(".v-messages__message").CountAsync() > 0;
    }

    public static void LogInvalid(string label, bool isInvalid)
        => Log.Information("Field Required: {Label} -> invalidCheck={Invalid}", label, isInvalid);
}