using System.ComponentModel;

namespace NiceEntry.Drawing;

// Public only because the MSBuild XAML compiler cannot consume internal types
// from the same assembly. Hidden from consumer IntelliSense.
[EditorBrowsable(EditorBrowsableState.Never)]
public class NotchedBorder : ContentView, IDrawable
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly BindableProperty StrokeColorProperty = BindableProperty.Create(
        nameof(StrokeColor), typeof(Color), typeof(NotchedBorder),
        defaultValue: Colors.Gray,
        propertyChanged: (b, _, v) =>
        {
            var nb = (NotchedBorder)b;
            nb._strokeColor = (Color)v;
            nb._graphicsView.Invalidate();
        });

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly BindableProperty StrokeThicknessProperty = BindableProperty.Create(
        nameof(StrokeThickness), typeof(double), typeof(NotchedBorder),
        defaultValue: 1.0,
        propertyChanged: (b, _, v) =>
        {
            var nb = (NotchedBorder)b;
            nb._strokeThickness = (float)(double)v;
            nb._graphicsView.Invalidate();
        });

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(
        nameof(CornerRadius), typeof(double), typeof(NotchedBorder),
        defaultValue: 8.0,
        propertyChanged: (b, _, v) =>
        {
            var nb = (NotchedBorder)b;
            nb._cornerRadius = (float)(double)v;
            nb._graphicsView.Invalidate();
        });

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly BindableProperty NotchStartProperty = BindableProperty.Create(
        nameof(NotchStart), typeof(double), typeof(NotchedBorder),
        defaultValue: 0.0,
        propertyChanged: (b, _, v) =>
        {
            var nb = (NotchedBorder)b;
            nb._notchStart = (float)(double)v;
            nb._graphicsView.Invalidate();
        });

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly BindableProperty NotchEndProperty = BindableProperty.Create(
        nameof(NotchEnd), typeof(double), typeof(NotchedBorder),
        defaultValue: 0.0,
        propertyChanged: (b, _, v) =>
        {
            var nb = (NotchedBorder)b;
            nb._notchEnd = (float)(double)v;
            nb._graphicsView.Invalidate();
        });

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly BindableProperty ContentPaddingProperty = BindableProperty.Create(
        nameof(ContentPadding), typeof(Thickness), typeof(NotchedBorder),
        defaultValue: new Thickness(0),
        propertyChanged: (b, _, _) => ((NotchedBorder)b)._contentHost.Padding = ((NotchedBorder)b).ContentPadding);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public Color StrokeColor { get => (Color)GetValue(StrokeColorProperty); set => SetValue(StrokeColorProperty, value); }
    [EditorBrowsable(EditorBrowsableState.Never)]
    public double StrokeThickness { get => (double)GetValue(StrokeThicknessProperty); set => SetValue(StrokeThicknessProperty, value); }
    [EditorBrowsable(EditorBrowsableState.Never)]
    public double CornerRadius { get => (double)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    [EditorBrowsable(EditorBrowsableState.Never)]
    public double NotchStart { get => (double)GetValue(NotchStartProperty); set => SetValue(NotchStartProperty, value); }
    [EditorBrowsable(EditorBrowsableState.Never)]
    public double NotchEnd { get => (double)GetValue(NotchEndProperty); set => SetValue(NotchEndProperty, value); }
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Thickness ContentPadding { get => (Thickness)GetValue(ContentPaddingProperty); set => SetValue(ContentPaddingProperty, value); }

    private readonly GraphicsView _graphicsView;
    private readonly Grid _contentHost;

    private Color _strokeColor = Colors.Gray;
    private float _strokeThickness = 1f;
    private float _cornerRadius = 8f;
    private float _notchStart;
    private float _notchEnd;

    public NotchedBorder()
    {
        _graphicsView = new GraphicsView
        {
            Drawable = this,
            InputTransparent = true
        };
        _contentHost = new Grid();
        var root = new Grid();
        root.Add(_graphicsView);
        root.Add(_contentHost);
        base.Content = root;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
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
        canvas.StrokeColor = _strokeColor;
        canvas.StrokeSize = _strokeThickness;
        canvas.Antialias = true;

        var path = NotchedBorderDrawing.BuildPath(
            dirtyRect.Width, dirtyRect.Height, _cornerRadius, _strokeThickness, _notchStart, _notchEnd);

        canvas.DrawPath(path);
    }
}
