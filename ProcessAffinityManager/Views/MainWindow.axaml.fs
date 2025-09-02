namespace ProcessAffinityManager.Views

open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml
open FluentAvalonia.UI.Controls
open ProcessAffinityManager.ViewModels

type MainWindow() as this =
    inherit Window()

    do
        this.InitializeComponent()

        this.GetObservable(Window.WindowStateProperty)
        |> Observable.add (fun state ->
            if state = WindowState.Minimized then
                this.Hide())

    override this.OnClosing(e: WindowClosingEventArgs) =
        e.Cancel <- true

        this.Hide()
        base.OnClosing e

    member private this.InitializeComponent() =
#if DEBUG
        this.AttachDevTools()
#endif
        AvaloniaXamlLoader.Load(this)
        
    member this.OnSelectionChanged (_sender: obj) (e: NavigationViewSelectionChangedEventArgs) =
        (this.DataContext :?> MainWindowViewModel).OnSelectionChanged _sender e
