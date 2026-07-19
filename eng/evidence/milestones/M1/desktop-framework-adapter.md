# FND-005 Desktop Framework Adapter

`	ext
Opure.Desktop.Contracts
    DesktopShellSnapshot
    IDesktopShellStateSource
    DesktopShellViewModel
             |
             | framework-neutral state
             v
Opure.Desktop
    DesktopShellComposition
    MainWindow.axaml
    Avalonia application lifetime
             |
             | presentation only
             v
Windows 11 window
`

## Boundary

- Opure.Desktop.Contracts has no Avalonia package or type dependency.
- Opure.Desktop is the Avalonia adapter.
- Runtime and domain projects do not reference Avalonia.
- The Desktop does not reference Runtime implementation projects.
- A WinUI 3 fallback can reuse the contracts and view model while replacing the adapter and views.