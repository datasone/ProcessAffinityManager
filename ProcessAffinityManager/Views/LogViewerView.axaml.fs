namespace ProcessAffinityManager.Views

open Avalonia.Controls
open Avalonia.Markup.Xaml

type LogViewerView() as this =
    inherit UserControl()

    do this.InitializeComponent()

    member private this.InitializeComponent() = AvaloniaXamlLoader.Load(this)
