namespace ProcessAffinityManager.Views

open Avalonia.Markup.Xaml
open FluentAvalonia.UI.Controls
open ProcessAffinityManager.ViewModels

type RuleEditorDialog() as this =
    inherit ContentDialog()

    do this.InitializeComponent()

    override _.StyleKeyOverride = typeof<ContentDialog>
    
    member private this.ViewModel = this.DataContext :?> RuleEditorViewModel

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)

    member this.GetRule = this.ViewModel.GetRule