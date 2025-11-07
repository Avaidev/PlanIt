using ReactiveUI;

namespace PlanIt.Services;

public class OverlayService : ReactiveObject
{
    private bool _isCategoryCreateVisible;
    public bool IsCategoryCreateVisible
    {
        get => _isCategoryCreateVisible;
        set => this.RaiseAndSetIfChanged(ref _isCategoryCreateVisible, value);
    }
    
    private bool _isTaskCreateVisible;
    public bool IsTaskCreateVisible
    {
        get => _isTaskCreateVisible;
        set => this.RaiseAndSetIfChanged(ref _isTaskCreateVisible, value);
    }
    
    public bool IsAnyVisible => IsCategoryCreateVisible || IsTaskCreateVisible;
}