using System;
using ReactiveUI;

namespace PlanIt.Models;

public class Panel : ReactiveObject
{
    private string _text = "";
    private string _icon = "";
    private string _color = "";
    private bool _visible = false;
    private bool _plusIsVisible = false;
    
    public Panel(){}

    public string Text
    {
        get => _text;
        set => this.RaiseAndSetIfChanged(ref _text, value);
    }

    public string Icon
    {
        get => _icon;
        set => this.RaiseAndSetIfChanged(ref _icon, value);
    }

    public string Color
    {
        get => _color;
        set => this.RaiseAndSetIfChanged(ref _color, value);
    }
    
    public bool Visible
    {
        get => _visible;
        set => this.RaiseAndSetIfChanged(ref _visible, value);
    }

    public bool PlusIsVisible
    {
        get => _plusIsVisible;
        set => this.RaiseAndSetIfChanged(ref _plusIsVisible, value);
    }
}