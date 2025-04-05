namespace InventoryManagment.Views;

public partial class DateSelectPage : ContentPage
{
    private readonly TaskCompletionSource<DateTime?> _tcs = new();

    public DateSelectPage()
    {
        InitializeComponent();
    }

    public Task<DateTime?> GetSelectedDateAsync() => _tcs.Task;

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        _tcs.SetResult(RemanentDatePicker.Date);
        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _tcs.SetResult(null);
        await Navigation.PopModalAsync();
    }
}