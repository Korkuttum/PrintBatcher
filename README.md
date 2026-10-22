# PrintBatcher

A lightweight Windows desktop app for batch-printing many files at once — add files or whole folders, pick a printer, and print the queue in one go.

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4) ![WPF](https://img.shields.io/badge/UI-WPF-0078D4) ![License: MIT](https://img.shields.io/badge/license-MIT-green)

## Features

- Add files or entire folders (recursively), or drag & drop them onto the window
- Checkbox-based queue: select which items actually get printed
- Printer selection with quick access to the printer's native properties dialog
- Direct rendering for raster images (jpg, png, bmp, tiff, gif) — no third-party engine required
- Every other file type (PDF, Office documents, text, etc.) is printed through its own default application via the Windows shell `printto` verb, the same mechanism Explorer's right-click → Print uses
- Export/import the file queue as a plain-text list
- Configurable settings: default copies, color mode, orientation, duplex, scaling, collate, confirmation prompts, skip-file masks, remembering the last printer/session, and more
- Settings and last session persist between runs (`%AppData%\PrintBatcher`)

## Getting started

### Run from source

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet build
dotnet run
```

### Build an installer

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

Then compile `installer/PrintBatcher.iss` with [Inno Setup](https://jrsoftware.org/isinfo.php) to produce `installer-output/PrintBatcher-Setup.exe`.

## Project layout

```
Models/        Data models (print job, settings, enums)
ViewModels/    MVVM view models and command plumbing
Views/         MainWindow.xaml, SettingsWindow.xaml
Services/      Printing engine and settings persistence
Converters/    WPF value converters
Assets/        App icon
installer/     Inno Setup installer script
```

## License

MIT — see [LICENSE](LICENSE).
