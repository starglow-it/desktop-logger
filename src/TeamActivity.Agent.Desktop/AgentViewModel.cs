using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace TeamActivity.Agent.Desktop;

public sealed class AgentViewModel : INotifyPropertyChanged
{
    private string trackingStatus = "Paused";
    private string lastSynchronization = "Waiting for enrollment";

    public AgentViewModel()
    {
        StartWorkCommand = new RelayCommand(() => TrackingStatus = "Working — activity telemetry on");
        StartBreakCommand = new RelayCommand(() => TrackingStatus = "Approved break");
        EndBreakCommand = new RelayCommand(() => TrackingStatus = "Working — activity telemetry on");
        EndWorkCommand = new RelayCommand(() => TrackingStatus = "Paused");
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public ICommand StartWorkCommand { get; }
    public ICommand StartBreakCommand { get; }
    public ICommand EndBreakCommand { get; }
    public ICommand EndWorkCommand { get; }

    public string TrackingStatus
    {
        get => trackingStatus;
        private set { trackingStatus = value; OnPropertyChanged(); }
    }

    public string LastSynchronization
    {
        get => lastSynchronization;
        private set { lastSynchronization = value; OnPropertyChanged(); }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

internal sealed class RelayCommand(Action execute) : ICommand
{
    public event EventHandler? CanExecuteChanged { add { } remove { } }
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => execute();
}
