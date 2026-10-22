using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using Microsoft.Win32;
using PrintBatcher.Models;
using PrintBatcher.Services;

namespace PrintBatcher.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly PrintService _printService = new();
    private CancellationTokenSource? _printCts;
    private readonly string? _lastPrinter;

    public ObservableCollection<PrintJob> Jobs { get; } = new();
    public ObservableCollection<string> Printers { get; } = new();
    public AppSettings Settings { get; }

    private PrintJob? _selectedJob;
    public PrintJob? SelectedJob
    {
        get => _selectedJob;
        set => SetField(ref _selectedJob, value);
    }

    private string? _selectedPrinter;
    public string? SelectedPrinter
    {
        get => _selectedPrinter;
        set
        {
            if (SetField(ref _selectedPrinter, value))
                PrinterPropertiesCommand?.RaiseCanExecuteChanged();
        }
    }

    private bool _isPrinting;
    public bool IsPrinting
    {
        get => _isPrinting;
        set
        {
            if (SetField(ref _isPrinting, value))
            {
                AddFilesCommand.RaiseCanExecuteChanged();
                AddFolderCommand.RaiseCanExecuteChanged();
                RemoveCheckedCommand.RaiseCanExecuteChanged();
                ClearAllCommand.RaiseCanExecuteChanged();
                ExportListCommand.RaiseCanExecuteChanged();
                ImportListCommand.RaiseCanExecuteChanged();
                StartCommand.RaiseCanExecuteChanged();
                CancelCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private int _completedCount;
    public int CompletedCount
    {
        get => _completedCount;
        set => SetField(ref _completedCount, value);
    }

    private string _statusText = "Hazır";
    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }

    public bool HasNoJobs => Jobs.Count == 0;

    public RelayCommand AddFilesCommand { get; }
    public RelayCommand AddFolderCommand { get; }
    public RelayCommand RemoveCheckedCommand { get; }
    public RelayCommand ClearAllCommand { get; }
    public RelayCommand ExportListCommand { get; }
    public RelayCommand ImportListCommand { get; }
    public RelayCommand PrinterPropertiesCommand { get; }
    public RelayCommand OpenSettingsCommand { get; }
    public RelayCommand StartCommand { get; }
    public RelayCommand CancelCommand { get; }

    public MainViewModel()
    {
        var persisted = SettingsService.Load();
        Settings = persisted.Settings;
        _lastPrinter = persisted.LastPrinter;

        RefreshPrinters();

        if (Settings.RestoreLastSession && persisted.LastSessionFiles.Count > 0)
            AddPaths(persisted.LastSessionFiles);

        AddFilesCommand = new RelayCommand(_ => AddFiles(), _ => !IsPrinting);
        AddFolderCommand = new RelayCommand(_ => AddFolder(), _ => !IsPrinting);
        RemoveCheckedCommand = new RelayCommand(_ => RemoveChecked(), _ => !IsPrinting && Jobs.Any(j => j.IsSelected));
        ClearAllCommand = new RelayCommand(_ => Jobs.Clear(), _ => !IsPrinting && Jobs.Count > 0);
        ExportListCommand = new RelayCommand(_ => ExportList(), _ => !IsPrinting && Jobs.Count > 0);
        ImportListCommand = new RelayCommand(_ => ImportList(), _ => !IsPrinting);
        PrinterPropertiesCommand = new RelayCommand(_ => PrintService.OpenPrinterProperties(SelectedPrinter!), _ => SelectedPrinter != null);
        OpenSettingsCommand = new RelayCommand(_ => OpenSettings());
        StartCommand = new RelayCommand(async _ => await StartPrintingAsync(), _ => !IsPrinting && Jobs.Count > 0 && SelectedPrinter != null);
        CancelCommand = new RelayCommand(_ => _printCts?.Cancel(), _ => IsPrinting);

        Jobs.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasNoJobs));
            ClearAllCommand.RaiseCanExecuteChanged();
            ExportListCommand.RaiseCanExecuteChanged();
            StartCommand.RaiseCanExecuteChanged();
        };
    }

    private void RefreshPrinters()
    {
        Printers.Clear();
        foreach (var printer in PrintService.GetInstalledPrinters())
            Printers.Add(printer);

        string? preferred = Settings.RememberLastPrinter && _lastPrinter != null && Printers.Contains(_lastPrinter)
            ? _lastPrinter
            : null;

        SelectedPrinter = preferred ?? PrintService.GetDefaultPrinter() ?? Printers.FirstOrDefault();
    }

    private void AddFiles()
    {
        var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Title = "Yazdırılacak dosyaları seçin",
            Filter = "Tüm dosyalar (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
            AddPaths(dialog.FileNames);
    }

    private void AddFolder()
    {
        var dialog = new OpenFolderDialog { Title = "Yazdırılacak klasörü seçin" };
        if (dialog.ShowDialog() == true)
            AddPaths(Directory.EnumerateFiles(dialog.FolderName, "*", SearchOption.AllDirectories));
    }

    public void AddPaths(IEnumerable<string> paths)
    {
        var skipPatterns = Settings.SkipFilesMask
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ToRegex)
            .ToList();

        foreach (var path in paths)
        {
            if (!File.Exists(path)) continue;
            if (Jobs.Any(j => j.FilePath.Equals(path, StringComparison.OrdinalIgnoreCase))) continue;

            string name = Path.GetFileName(path);
            if (skipPatterns.Any(p => p.IsMatch(name))) continue;

            var job = new PrintJob(path) { Copies = Settings.DefaultCopies };
            Jobs.Add(job);
        }
    }

    private static Regex ToRegex(string wildcardMask)
    {
        string pattern = "^" + Regex.Escape(wildcardMask).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        return new Regex(pattern, RegexOptions.IgnoreCase);
    }

    private void RemoveChecked()
    {
        foreach (var job in Jobs.Where(j => j.IsSelected).ToList())
            Jobs.Remove(job);
    }

    private void ExportList()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Belge listesini dışa aktar",
            Filter = "Metin dosyası (*.txt)|*.txt",
            FileName = "belge-listesi.txt"
        };

        if (dialog.ShowDialog() == true)
            File.WriteAllLines(dialog.FileName, Jobs.Select(j => j.FilePath));
    }

    private void ImportList()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Belge listesini içe aktar",
            Filter = "Metin dosyası (*.txt)|*.txt"
        };

        if (dialog.ShowDialog() == true)
            AddPaths(File.ReadAllLines(dialog.FileName));
    }

    private void OpenSettings()
    {
        var window = new SettingsWindow(Settings)
        {
            Owner = Application.Current.MainWindow
        };
        window.ShowDialog();
    }

    private async Task StartPrintingAsync()
    {
        if (SelectedPrinter is null) return;

        var jobsToPrint = Jobs.Where(j => j.IsSelected).ToList();
        if (jobsToPrint.Count == 0) return;

        if (Settings.ConfirmBeforePrinting)
        {
            var result = MessageBox.Show(
                $"{jobsToPrint.Count} dosya '{SelectedPrinter}' yazıcısına gönderilecek. Devam edilsin mi?",
                "Yazdırmayı Onayla", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;
        }

        _printCts = new CancellationTokenSource();
        IsPrinting = true;
        CompletedCount = 0;
        int total = jobsToPrint.Count;

        try
        {
            foreach (var job in jobsToPrint)
            {
                if (_printCts.IsCancellationRequested)
                {
                    if (job.Status == PrintStatus.Pending)
                        job.Status = PrintStatus.Skipped;
                    continue;
                }

                StatusText = $"Gönderiliyor: {job.FileName} ({CompletedCount + 1}/{total})";

                try
                {
                    if (Settings.WaitBeforeEachDocumentMs > 0)
                        await Task.Delay(Settings.WaitBeforeEachDocumentMs, _printCts.Token);

                    await _printService.PrintAsync(job.FilePath, SelectedPrinter, job.Copies, Settings, _printCts.Token);

                    // We only know the job was handed to the printer, not that it actually
                    // finished printing (the source app could still crash afterwards) — so
                    // the status reflects "sent", never a falsely confident "done".
                    job.Status = PrintStatus.Printing;

                    if (Settings.RemovePrintedFromList)
                        Jobs.Remove(job);
                }
                catch (OperationCanceledException)
                {
                    job.Status = PrintStatus.Skipped;
                }
                catch (Exception ex)
                {
                    job.Status = PrintStatus.Failed;
                    job.ErrorMessage = ex.Message;

                    if (!Settings.SkipFilesWithErrors)
                        break;
                }

                CompletedCount++;
            }
        }
        finally
        {
            IsPrinting = false;
            StatusText = _printCts.IsCancellationRequested ? "İptal edildi" : "Tamamlandı";
            _printCts.Dispose();
            _printCts = null;

            if (Settings.ClearListAfterPrinting && !_isPrinting)
                Jobs.Clear();
        }
    }

    public void SaveState()
    {
        SettingsService.Save(new PersistedState
        {
            Settings = Settings,
            LastPrinter = SelectedPrinter,
            LastSessionFiles = Jobs.Select(j => j.FilePath).ToList()
        });
    }
}
