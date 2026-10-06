using Godot;
using System;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

public partial class ClearAllButton : NButton
{ 
    public override void _Ready()
    {
        ConnectSignals();
    }
}
