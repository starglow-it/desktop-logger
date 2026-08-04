using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Threading;
using TeamActivity.Contracts;

namespace TeamActivity.Manager.Desktop;

public sealed class DashboardViewModel : INotifyPropertyChanged
{
    private readonly HttpClient client = new() { BaseAddress = new Uri("http://127.0.0.1:7443") };
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(15) };
    private string serverState = "Connecting…";
    private bool refreshing;

    public DashboardViewModel()
    {
        jsonOptions.Converters.Add(new JsonStringEnumConverter());
        timer.Tick += async (_, _) => await RefreshAsync();
        timer.Start();
        _ = RefreshAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<DeviceSummary> Devices { get; } = [];
    public int DeviceCount => Devices.Count;
    public int OnlineCount => Devices.Count(x => x.State == DeviceConnectionState.Online);
    public int AlertCount => Devices.Sum(x => x.OpenAlertCount);

    public string ServerState
    {
        get => serverState;
        private set { serverState = value; OnPropertyChanged(); }
    }

    private async Task RefreshAsync()
    {
        if (refreshing) return;
        refreshing = true;
        try
        {
            var devices = await client.GetFromJsonAsync<List<DeviceSummary>>("/api/devices", jsonOptions) ?? [];
            Devices.Clear();
            foreach (var device in devices) Devices.Add(device);
            ServerState = "Server online";
            OnPropertyChanged(nameof(DeviceCount));
            OnPropertyChanged(nameof(OnlineCount));
            OnPropertyChanged(nameof(AlertCount));
        }
        catch (HttpRequestException)
        {
            ServerState = "Server unavailable";
        }
        finally
        {
            refreshing = false;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
