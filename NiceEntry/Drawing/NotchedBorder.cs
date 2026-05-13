using Microsoft.Maui.Graphics;

namespace NiceEntry.Drawing;

public class NotchedBorder : ContentView, IDrawable
{
    public static readonly BindableProperty StrokeColorProperty = BindableProperty.Create(
        nameof(StrokeColor), typeof(Color), typeof(NotchedBorder),
        defaultValue: Colors.Gray,
        propertyChanged: (b, _, _) => ((NotchedBorder)b)._graphicsView.Invalidate());

    public static readonly BindableProperty StrokeThicknessProperty = BindableProperty.Create(
        nameof(StrokeThickness), typeof(double), typeof(NotchedBorder),
        defaultValue: 1.0,
        propertyChanged: (b, _, _) => ((NotchedBorder)b)._graphicsView.Invalidate());

    public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(
        nameof(CornerRadius), typeof(double), typeof(NotchedBorder),
        defaultValue: 8.0,
        propertyChanged: (b, _, _) => ((NotchedBorder)b)._graphicsView.Invalidate());

    public static readonly BindableProperty NotchStartProperty = BindableProperty.Create(
        nameof(NotchStart), typeof(double), typeof(NotchedBorder),
        defaultValue: 0.0,
        propertyChanged: (b, _, _) => ((NotchedBorder)b)._graphicsView.Invalidate());

    public static readonly BindableProperty NotchEndProperty = BindableProperty.Create(
        nameof(NotchEnd), typeof(double), typeof(NotchedBorder),
        defaultValue: 0.0,
        propertyChanged: (b, _, _) => ((NotchedBorder)b)._graphicsView.Invalidate());

    public static readonly BindableProperty ContentPaddingProperty = BindableProperty.Create(
        nameof(ContentPadding), typeof(Thickness), typeof(NotchedBorder),
        defaultValue: new Thickness(0),
        propertyChanged: (b, _, _) => ((NotchedBorder)b)._contentHost.Padding = ((NotchedBorder)b).ContentPadding);

    public Color StrokeColor { get => (Color)GetValue(StrokeColorProperty); set => SetValue(StrokeColorProperty, value); }
    public double StrokeThickness { get => (double)GetValue(StrokeThicknessProperty); set => SetValue(StrokeThicknessProperty, value); }
    public double CornerRadius { get => (double)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public double NotchStart { get => (double)GetValue(NotchStartProperty); set => SetValue(NotchStartProperty, value); }
    public double NotchEnd { get => (double)GetValue(NotchEndProperty); set => SetValue(NotchEndProperty, value); }
    public Thickness ContentPadding { get => (Thickness)GetValue(ContentPaddingProperty); set => SetValue(ContentPaddingProperty, value); }

    private readonly GraphicsView _graphicsView;
    private readonly Grid _contentHost;
    private readonly Grid _root;

    public NotchedBorder()
    {
        _graphicsView = new GraphicsView
        {
            Drawable = this,
            InputTransparent = true
        };
        _contentHost = new Grid();
        _root = new Grid();
        _root.Add(_graphicsView);
        _root.Add(_contentHost);
        base.Content = _root;
    }

    public new View? Content
    {
        get => _contentHost.Children.Count > 0 ? (View)_contentHost.Children[0] : null;
        set
        {
            _contentHost.Children.Clear();
            if (value is not null) _contentHost.Children.Add(value);
        }
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var stroke = (float)StrokeThickness;
        var radius = (float)CornerRadius;
        var notchStart = (float)NotchStart;
        var notchEnd = (float)NotchEnd;

        canvas.StrokeColor = StrokeColor;
        canvas.StrokeSize = stroke;
        canvas.Antialias = true;

        var path = NotchedBorderDrawing.BuildPath(
            dirtyRect.Width, dirtyRect.Height, radius, stroke, notchStart, notchEnd);

        canvas.DrawPath(path);
    }
}
