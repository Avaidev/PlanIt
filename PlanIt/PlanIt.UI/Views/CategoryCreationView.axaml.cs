using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using PlanIt.UI.ViewModels;
using ReactiveUI.Avalonia;

namespace PlanIt.Views;

public partial class CategoryCreationView : ReactiveUserControl<CategoryManagerViewModel>
{
    public CategoryCreationView()
    {
        InitializeComponent();
    }
}