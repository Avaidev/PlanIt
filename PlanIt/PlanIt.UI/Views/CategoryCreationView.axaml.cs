using PlanIt.UI.ViewModels;
using ReactiveUI.Avalonia;

namespace PlanIt.UI.Views;

public partial class CategoryCreationView : ReactiveUserControl<CategoryManagerViewModel>
{
    public CategoryCreationView()
    {
        InitializeComponent();
    }
}