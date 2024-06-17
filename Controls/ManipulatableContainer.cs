﻿using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;

namespace IconExtractor.Controls;

/// <summary>
/// This class represents a XAML element that can be manipulated through translation, rotation, and scaling.
/// </summary>
/// <remarks>
/// This class overrides any RenderTransform applied to it.
/// </remarks>
class ManipulatableContainer : ContentControl
{
    private CompositeTransform? _transform;

    public ManipulatableContainer()
    {
        this.Loaded += ManipulatableContainer_Loaded;
    }

    void ManipulatableContainer_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _transform = new CompositeTransform();
        this.RenderTransform = _transform;
        // Enable standard manipulations, without inertia to ensure they stay in view
        this.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY | ManipulationModes.Rotate | ManipulationModes.Scale;
    }

    protected override void OnManipulationStarting(ManipulationStartingRoutedEventArgs e)
    {
        base.OnManipulationStarting(e);
        e.Handled = true;
    }

    protected override void OnManipulationStarted(ManipulationStartedRoutedEventArgs e)
    {
        base.OnManipulationStarted(e);
        e.Handled = true;
    }

    protected override void OnManipulationDelta(ManipulationDeltaRoutedEventArgs e)
    {
        base.OnManipulationDelta(e);

        if (_transform is null)
            return;

        // Update render transform to reflect manipulation deltas.
        _transform.Rotation += e.Delta.Rotation;
        _transform.ScaleX += e.Delta.Scale - 1.0f;
        _transform.ScaleY += e.Delta.Scale - 1.0f;
        _transform.TranslateX += e.Delta.Translation.X;
        _transform.TranslateY += e.Delta.Translation.Y;

        e.Handled = true;
    }

    protected override void OnManipulationCompleted(ManipulationCompletedRoutedEventArgs e)
    {
        base.OnManipulationCompleted(e);
        e.Handled = true;
    }
}
