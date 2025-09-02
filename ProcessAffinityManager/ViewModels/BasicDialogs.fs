namespace ProcessAffinityManager.ViewModels

open Avalonia.Media
open FluentAvalonia.UI.Controls

module BasicDialogs =
    let questionDialog parent header content =
        let dialog = TaskDialog(
            Header = header,
            Content = content,
            FooterVisibility = TaskDialogFooterVisibility.Never,
            IsFooterExpanded = false,
            ShowProgressBar = false,
            XamlRoot = parent,
            IconSource = SymbolIconSource(Symbol = Symbol.Help, Foreground = Brushes.Teal)
        )
        
        let yesBtn = TaskDialogButton(Text = "Yes", DialogResult = TaskDialogStandardResult.Yes)
        let noBtn = TaskDialogButton(Text = "No", DialogResult = TaskDialogStandardResult.No)
        
        dialog.Buttons.Add(yesBtn)
        dialog.Buttons.Add(noBtn)
        
        dialog