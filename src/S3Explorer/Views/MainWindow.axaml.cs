using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using S3Explorer.ViewModels;

#pragma warning disable CS0618 // Avalonia DragDrop API deprecations

namespace S3Explorer.Views;

public partial class MainWindow : Window
{
    private Point _dragStartPoint;
    private bool _isDragInProgress;

    public MainWindow()
    {
        InitializeComponent();

        var grid = this.FindControl<DataGrid>("ObjectGrid")!;
        grid.AddHandler(DragDrop.DropEvent, OnDrop);
        grid.AddHandler(DragDrop.DragOverEvent, OnDragOver);

        AddHandler(PointerPressedEvent, OnGridPointerPressed, handledEventsToo: true);
        AddHandler(PointerMovedEvent, OnGridPointerMoved, handledEventsToo: true);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ActivityLog.CollectionChanged += OnActivityLogChanged;
        }
    }

    private void OnActivityLogChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            var listBox = this.FindControl<ListBox>("ActivityLogList");
            if (listBox != null && listBox.ItemCount > 0)
            {
                listBox.ScrollIntoView(listBox.ItemCount - 1);
            }
        }
    }

    private void ObjectGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && vm.SelectedObject != null)
        {
            vm.DoubleClickObjectCommand.Execute(vm.SelectedObject);
        }
    }

    // --- Drop (explorer → app) ---

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.Data.Contains(DataFormats.Files)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        if (!e.Data.Contains(DataFormats.Files)) return;

        var items = e.Data.GetFiles();
        if (items == null) return;

        var paths = new List<string>();
        foreach (var item in items)
        {
            var path = item.TryGetLocalPath();
            if (path != null)
                paths.Add(path);
        }

        if (paths.Count > 0)
            await vm.UploadDroppedItemsAsync(paths);
    }

    // --- Drag out (app → explorer) ---

    private void OnGridPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _dragStartPoint = e.GetPosition(this);
            _isDragInProgress = false;
        }
    }

    private async void OnGridPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDragInProgress) return;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        // Only start drag from the object grid area
        var grid = this.FindControl<DataGrid>("ObjectGrid")!;
        var posInGrid = e.GetPosition(grid);
        if (posInGrid.X < 0 || posInGrid.Y < 0 ||
            posInGrid.X > grid.Bounds.Width || posInGrid.Y > grid.Bounds.Height)
            return;

        var currentPos = e.GetPosition(this);
        if (Math.Abs(currentPos.X - _dragStartPoint.X) < 10 &&
            Math.Abs(currentPos.Y - _dragStartPoint.Y) < 10)
            return;

        if (DataContext is not MainWindowViewModel vm) return;
        var item = vm.SelectedObject;
        if (item == null || item.IsPrefix) return;

        _isDragInProgress = true;

        try
        {
            var tempPath = await vm.DownloadToTempAsync(item);
            if (tempPath == null) return;

            var storageFile = await StorageProvider.TryGetFileFromPathAsync(new Uri("file:///" + tempPath.Replace('\\', '/')));
            if (storageFile == null) return;

            var data = new DataObject();
            data.Set(DataFormats.Files, new[] { storageFile });
            await DragDrop.DoDragDrop(e, data, DragDropEffects.Copy);
        }
        finally
        {
            _isDragInProgress = false;
        }
    }
}
