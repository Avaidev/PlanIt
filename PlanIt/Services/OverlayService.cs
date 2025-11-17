using System;
using PlanIt.Models;
using ReactiveUI;

namespace PlanIt.Services;

public class OverlayService : ReactiveObject
{
    private bool _isCategoryCreateVisible;
    private bool _isTaskCreateVisible;
    private bool _editMode;
    
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

    public bool EditMode
    {
        get => _editMode;
        set => this.RaiseAndSetIfChanged(ref _editMode, value);
    }
    
    public bool IsAnyVisible => IsCategoryCreateVisible || IsTaskCreateVisible;

    public void ToggleVisibility(int overlay, bool edit=false)
    {
        EditMode = edit;
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