namespace ProcessAffinityManager.Services

open System.Collections.Generic
open Avalonia
open Avalonia.Data.Converters
open Avalonia.Media
open Avalonia.Styling
open ProcessAffinityManager.Models

type EnumToBooleanConverter() =
    interface IValueConverter with
        member this.Convert(value, _targetType, parameter, _culture) =
            let paramString = parameter :?> string
            let valueString = value.ToString()
            box (valueString.Equals(paramString))

        member this.ConvertBack(value, targetType, parameter, _culture) =
            let paramString = parameter :?> string

            if value :?> bool then
                match targetType with
                | _t when _t = typeof<ProfileType> ->
                    match paramString with
                    | "CPUAffinity" -> CPUAffinity
                    | "CPUSet" -> CPUSet
                    | _ -> invalidArg paramString "Invalid parameter"
                | _ -> invalidArg (targetType.ToString()) "Invalid type"
            else
                Avalonia.Data.BindingOperations.DoNothing

type TupleConverter() =
    interface IMultiValueConverter with
        member this.Convert(values: IList<obj>, _targetType, _parameter, _culture) =
            (values[0], values[1])
            
type LogLevelToBrushConverter() =
    interface IValueConverter with
        member this.Convert(value, _targetType, _parameter, _culture) =
            match value with
            | :? LogLevel as level ->
                let color =
                    match level with
                    | LogLevel.Info ->
                        if Application.Current.ActualThemeVariant = ThemeVariant.Light then
                            Brushes.Black
                        else
                            Brushes.White
                    | LogLevel.Warning -> Brushes.Orange
                    | LogLevel.ErrorLog -> Brushes.Red

                box color
            | _ -> box Brushes.Black

        member this.ConvertBack(_value, _targetType, _parameter, _culture) =
            Avalonia.Data.BindingOperations.DoNothing