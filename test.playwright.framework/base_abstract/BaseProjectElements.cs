using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Serilog;
using test.playwright.framework.constants;
using test.playwright.framework.utils;

namespace test.playwright.framework.base_abstract;

public class BaseProjectElements(IPage page) : AssertUtils(page)
{
    protected async Task SetupJavaScriptDialogHandlerAsync(string action = "Accept")
    {
        Page.Dialog -= OnDialogHandled;
        Page.Dialog += OnDialogHandled;
        return;

        async void OnDialogHandled(object? sender, IDialog dialog)
        {
            Log.Information($"Dialog triggered with message: {dialog.Message}");
            if (action.Equals("Accept", StringComparison.OrdinalIgnoreCase))
            {
                await dialog.AcceptAsync();
            }
            else
            {
                await dialog.DismissAsync();
            }

            Page.Dialog -= OnDialogHandled;
        }
    }

    private async Task<T> ClickButton<T>(string buttonSelector) where T : BaseProjectElements
    {
        await WaitForSelectorToExistAsync(buttonSelector);
        await Click(buttonSelector);
        return (T)Activator.CreateInstance(typeof(T), Page)!;
    }

    private async Task<T> InputValueTo<T>(string inputSelector, string text) where T : BaseProjectElements
    {
        await WaitForSelectorToExistAsync(inputSelector);
        await Input(inputSelector, text);
        return (T)Activator.CreateInstance(typeof(T), Page)!;
    }

    public async Task RefreshPage()
    {
        await Page.ReloadAsync();
        await WaitForNetworkIdle();
    }

    public async Task ClickEscapeByKeyboard()
    {
        await Page.Keyboard.PressAsync("Escape");
        Log.Information("Clicked 'Escape' button by keyword");
        await WaitForNetworkIdle();
    }

    protected async Task ClickEnterByKeyboard()
    {
        await Page.Keyboard.PressAsync("Enter");
        Log.Information("Clicked 'Enter' button by keyword");
        await WaitForNetworkIdle();
    }

    public async Task ClickGoToPreviousPage()
    {
        await Page.GoBackAsync();
        await WaitForNetworkIdle();
    }

    public async Task UploadFileAsync(string selectFilesField)
    {
        try
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), TestDataConstants.FilesToUploadDirectory,
                TestDataConstants.FileNameToUpload);

            Log.Information($"Attempting to upload file: {filePath} using selector: {selectFilesField}");
            await Page.SetInputFilesAsync(selectFilesField, filePath);
            Log.Information($"File upload attempted for: {TestDataConstants.FileNameToUpload}");
        }
        catch (Exception ex)
        {
            Log.Error(
                $"An error occurred while attempting to upload file: {TestDataConstants.FileNameToUpload}. Error: {ex.Message}");
            throw;
        }
    }

    public static int ParseEntityCount(string text, string entityName)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Log.Warning($"Input text is null or empty for parsing {entityName} count.");
            return 0;
        }

        var pattern = $@"{entityName}\s*\((\d+)\)";
        var match = Regex.Match(text, pattern);

        if (match.Success)
        {
            Log.Information($"Successfully parsed {entityName} count: {match.Groups[1].Value} from text: '{text}'.");
            return int.Parse(match.Groups[1].Value);
        }
        else
        {
            Log.Warning($"Failed to parse {entityName} count from text: '{text}'.");
            return 0;
        }
    }
}