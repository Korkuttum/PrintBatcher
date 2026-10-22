using PrintBatcher.ViewModels;

namespace PrintBatcher.Models;

public class AppSettings : ViewModelBase
{
    // ----- Genel -----
    private int _defaultCopies = 1;
    public int DefaultCopies
    {
        get => _defaultCopies;
        set => SetField(ref _defaultCopies, Math.Max(1, value));
    }

    private bool _confirmBeforePrinting = true;
    public bool ConfirmBeforePrinting
    {
        get => _confirmBeforePrinting;
        set => SetField(ref _confirmBeforePrinting, value);
    }

    private bool _skipFilesWithErrors = true;
    public bool SkipFilesWithErrors
    {
        get => _skipFilesWithErrors;
        set => SetField(ref _skipFilesWithErrors, value);
    }

    private bool _clearListAfterPrinting;
    public bool ClearListAfterPrinting
    {
        get => _clearListAfterPrinting;
        set => SetField(ref _clearListAfterPrinting, value);
    }

    private bool _removePrintedFromList;
    public bool RemovePrintedFromList
    {
        get => _removePrintedFromList;
        set => SetField(ref _removePrintedFromList, value);
    }

    private string _skipFilesMask = "~$*.*;*.db;desktop.ini";
    public string SkipFilesMask
    {
        get => _skipFilesMask;
        set => SetField(ref _skipFilesMask, value);
    }

    private int _waitBeforeEachDocumentMs;
    public int WaitBeforeEachDocumentMs
    {
        get => _waitBeforeEachDocumentMs;
        set => SetField(ref _waitBeforeEachDocumentMs, Math.Max(0, value));
    }

    // ----- Yazdırma (yalnızca görsel dosyalarda doğrudan uygulanır; diğer formatlar
    // yazıcının güncel sürücü varsayılanlarını kullanır) -----
    private ColorMode _colorMode = ColorMode.Auto;
    public ColorMode ColorMode
    {
        get => _colorMode;
        set => SetField(ref _colorMode, value);
    }

    private PageOrientation _orientation = PageOrientation.Auto;
    public PageOrientation Orientation
    {
        get => _orientation;
        set => SetField(ref _orientation, value);
    }

    private DuplexMode _duplex = DuplexMode.Simplex;
    public DuplexMode Duplex
    {
        get => _duplex;
        set => SetField(ref _duplex, value);
    }

    private bool _collate = true;
    public bool Collate
    {
        get => _collate;
        set => SetField(ref _collate, value);
    }

    private ScaleMode _scaleMode = ScaleMode.FitToPage;
    public ScaleMode ScaleMode
    {
        get => _scaleMode;
        set => SetField(ref _scaleMode, value);
    }

    private int _customScalePercent = 100;
    public int CustomScalePercent
    {
        get => _customScalePercent;
        set => SetField(ref _customScalePercent, Math.Clamp(value, 10, 400));
    }

    // ----- Arayüz -----
    private bool _rememberLastPrinter = true;
    public bool RememberLastPrinter
    {
        get => _rememberLastPrinter;
        set => SetField(ref _rememberLastPrinter, value);
    }

    private bool _restoreLastSession;
    public bool RestoreLastSession
    {
        get => _restoreLastSession;
        set => SetField(ref _restoreLastSession, value);
    }
}
