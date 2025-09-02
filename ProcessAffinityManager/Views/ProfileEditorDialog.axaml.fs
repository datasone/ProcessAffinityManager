namespace ProcessAffinityManager.Views

open Avalonia.Markup.Xaml
open FluentAvalonia.UI.Controls
open ProcessAffinityManager.ViewModels

type ProfileEditorDialog() as this =
    inherit ContentDialog()
        
    do this.InitializeComponent()
    
    override _.StyleKeyOverride = typeof<ContentDialog>

    member private this.ViewModel = this.DataContext :?> ProfileEditorViewModel

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)

    member this.GetProfile = this.ViewModel.GetProfile