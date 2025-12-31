# ViewModels

MVVM ViewModels supporting custom Avalonia editor UI components for game data editing.

## Overview

This directory contains ViewModels that follow the Model-View-ViewModel pattern, providing data binding and business logic for custom Avalonia views. Currently focused on the TeamMemberSpawn editor.

## Key Concepts

- **ViewModelBase**: Base class from RogueEssence.Dev.ViewModels providing `INotifyPropertyChanged` support
- **ReactiveUI**: Uses `RaiseAndSetIfChanged` and `SetIfChanged` for reactive property updates
- **CollectionBoxViewModel**: Reusable component for editing lists with add/edit/delete operations

## Related

- [../Views/](../Views/) - Avalonia XAML views that consume these ViewModels
- [../Editors/](../Editors/) - Custom editors that instantiate ViewModels
