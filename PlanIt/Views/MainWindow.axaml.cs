using System.Reactive;
using System.Reactive.Disposables.Fluent;
using Avalonia;
using Avalonia.Controls;
using PlanIt.ViewModels;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace PlanIt.Views;

public partial class MainWindow : ReactiveWindow<MainViewModel>

{
    public MainWindow()
    {
        InitializeComponent();

        this.WhenActivated(disposable =>
        {
            ViewModel.WindowVM.ShowCategoriesInteraction.RegisterHandler(interaction =>
            {
                var categories = interaction.Input;
                CategoryListBox.Items.Clear();
                foreach (var category in categories)
                {
                    CategoryListBox.Items.Add(category);
                }
                CategoryListBox.SelectedIndex = 0;
                interaction.SetOutput(Unit.Default);
            }).DisposeWith(disposable);
            
            ViewModel.WindowVM.ShowTasksInteraction.RegisterHandler(interaction =>
            {
                var tasks = interaction.Input;
                TasksItemsControl.Items.Clear();
                foreach (var task in tasks)
                {
                    TasksItemsControl.Items.Add(task);
                }
                interaction.SetOutput(Unit.Default);
            }).DisposeWith(disposable);
            
            ViewModel.CategoryCreationVM.AddCategoryInteraction.RegisterHandler(interaction =>
            {
                var category = interaction.Input;
                CategoryListBox.Items.Add(category);
                interaction.SetOutput(Unit.Default);
            }).DisposeWith(disposable);

            ViewModel.WindowVM.RemoveCategoryInteraction.RegisterHandler(interaction =>
            {
                var category = interaction.Input;
                CategoryListBox.Items.Remove(category);
                interaction.SetOutput(Unit.Default);
            }).DisposeWith(disposable);

            ViewModel.WindowVM.LoadCategoriesAsync();

#if DEBUG
            this.AttachDevTools();
#endif
        });
    }
}