using System;
using PlanIt.Models;
using ReactiveUI;

namespace PlanIt.Services;

public class OverlayService : ReactiveObject
{
    // Control attributes
    //      private
    private bool _isCategoryCreateVisible;
    private bool _isTaskCreateVisible;
    
    //      public
    public bool IsCategoryCreateVisible
    {
        get => _isCategoryCreateVisible;
        set => this.RaiseAndSetIfChanged(ref _isCategoryCreateVisible, value);
    }
    public bool IsTaskCreateVisible
    {
        get => _isTaskCreateVisible;
        set => this.RaiseAndSetIfChanged(ref _isTaskCreateVisible, value);
    }
    
    public bool IsAnyVisible => IsCategoryCreateVisible || IsTaskCreateVisible;

    // Commands
    public void ToggleVisibility(int overlay)
    {
        switch (overlay)
        {
            case 0:
                IsCategoryCreateVisible ^= true;
                break;
            case 1:
                IsTaskCreateVisible ^= true;
                break;
            default:
                throw new Exception("[OverlayService] Invalid overlay");
        }
    }
}