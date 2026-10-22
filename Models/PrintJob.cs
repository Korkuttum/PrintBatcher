using System.IO;
using PrintBatcher.ViewModels;

namespace PrintBatcher.Models;

public class PrintJob : ViewModelBase
{
    public string FilePath { get; }
    public string FileName => Path.GetFileName(FilePath);
    public string Extension => Path.GetExtension(FilePath).TrimStart('.').ToUpperInvariant();
    public DateTime DateAdded { get; } = DateTime.Now;

    private bool _isSelected = true;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    private int _copies = 1;
    public int Copies
    {
        get => _copies;
        set => SetField(ref _copies, Math.Max(1, value));
    }

    private PrintStatus _status = PrintStatus.Pending;
    public PrintStatus Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetField(ref _errorMessage, value);
    }

    public PrintJob(string filePath)
    {
        FilePath = filePath;
    }
}
