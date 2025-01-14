﻿using Microsoft.Win32;
using MinoriDemo.RibbonWPF.Modules.VirtualCanvas.Extensions;
using MinoriEditorStudio.Services;
using MinoriEditorStudio.VirtualCanvas.Controls;
using MinoriEditorStudio.VirtualCanvas.Service;
using MvvmCross;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MinoriDemo.RibbonWPF.Modules.VirtualCanvas.Models
{
    /// <summary>
    /// This demo shows the VirtualCanvas managing up to 50,000 random WPF shapes providing smooth scrolling and
    /// zooming while creating those shapes on the fly.  This helps make a WPF canvas that is a lot more
    /// scalable.
    /// </summary>
    public class VirtualCanvasModel : MvxNotifyPropertyChanged
    {
        //private readonly Boolean _animateStatus = true;
        //private readonly Int32 _totalVisuals = 0;

        private readonly Double _tileWidth = 50;
        private readonly Double _tileHeight = 30;
        private readonly Double _tileMargin = 10;
        private Int32 rows;
        private Int32 cols;
        private Boolean _showGridLines;
        private readonly Polyline _gridLines = new Polyline();

        public EventHandler IsClosing;

        public Boolean ShowContextRibbon => true;

        public ICommand OnHelpCommand => new MvxCommand(() =>
        {
            MessageBox.Show(
                "Click left mouse button and drag to pan the view " +
                "Hold Control-Key and run mouse wheel to zoom in and out " +
                "Click middle mouse button to turn on auto-scrolling " +
                "Hold Control-Key and drag the mouse with left button down to draw a rectangle to zoom into that region.",
                "User Interface", MessageBoxButton.OK, MessageBoxImage.Information);
        });

        public ICommand DumpQuadTreeCommand => new MvxCommand(() =>
        {
            SaveFileDialog s = new SaveFileDialog
            {
                FileName = "quadtree.xml"
            };
            if (s.ShowDialog() == true)
            {
                using (StreamWriter w = new StreamWriter(s.FileName))
                {
                    using (LogWriter log = new LogWriter(w))
                    {
                        log.Open("QuadTree");
                        Canvas.Graph.Index.Dump(log);
                        log.Open("Other");
                        log.WriteAttribute("MaxDepth", log.MaxDepth.ToString(CultureInfo.CurrentUICulture));
                        log.Close();
                        log.Close();
                    }
                }
            }
        });

        public ICommand RowColChange => new MvxCommand<Object>((x) =>
        {
            Int32 value = Int32.Parse(x.ToString());
            rows = cols = value;
            AllocateNodes();
        });

        public ICommand ZoomCommand => new MvxCommand<String>((x) =>
        {
            if (x == "Fit")
            {
                ResetZoom();
            }
            else
            {
                Double value = Double.Parse(x);
                Canvas.Zoom.Zoom = value / 100;
                _statusbar.Text = $"Zoom is {value}";
            }
        });

        private void ResetZoom()
        {
            Double scaleX = Canvas.Graph.ViewportWidth / Canvas.Graph.Extent.Width;
            Double scaleY = Canvas.Graph.ViewportHeight / Canvas.Graph.Extent.Height;
            Canvas.Zoom.Zoom = Math.Min(scaleX, scaleY);
            Canvas.Zoom.Offset = new Point(0, 0);
        }

        public Double ZoomValue
        {
            get => Canvas.Zoom?.Zoom ?? 0;
            set => Canvas.Zoom.Zoom = value;
        }


        public VirtualCanvasModel(IVirtualCanvas canvas)
        {
            Canvas = canvas;
            Canvas.CanClose = false;
            Canvas.DisplayName = "Virtual Canvas Sample";

            // Update Statusbar
            _statusbar = Mvx.IoCProvider.Resolve<IStatusBar>();
            _statusbar.Text = "Loading";

            // Override ctrl with alt. (Test code)
            Canvas.RectZoom.ModifierKeys = ModifierKeys.Alt;

            Canvas.Zoom.ZoomChanged += (s, e) =>
            {
                RaisePropertyChanged("ZoomValue");
                _statusbar.Text = $"Zoom:{ZoomValue}";
            };

            Canvas.RectZoom.ZoomReset += (s, e) => ResetZoom();

            Canvas.Graph.SmallScrollIncrement = new Size(_tileWidth + _tileMargin, _tileHeight + _tileMargin);
            Canvas.Graph.Scale.Changed += new EventHandler(OnScaleChanged);
            Canvas.Graph.Translate.Changed += new EventHandler(OnScaleChanged);

            // Origianlly 100 x 100 nodes
            AllocateNodes();

            // Update info 
            _statusbar.Text = "Ready";
        }

        private void AllocateNodes()
        {
            Canvas.Zoom.Zoom = 1;
            Canvas.Zoom.Offset = new Point(0, 0);

            // Fill a sparse grid of rectangular color palette nodes with each tile being 50x30.
            // with hue across x-axis and saturation on y-axis, brightness is fixed at 100;
            Random r = new Random(Environment.TickCount);
            Canvas.Graph.VirtualChildren.Clear();
            Double w = _tileWidth + _tileMargin;
            Double h = _tileHeight + _tileMargin;
            Int32 count = (rows * cols) / 20;
            Double width = (w * (cols - 1));
            Double height = (h * (rows - 1));
            while (count > 0)
            {
                Double x = r.NextDouble() * width;
                Double y = r.NextDouble() * height;

                Point pos = new Point(_tileMargin + x, _tileMargin + y);
                Size s = new Size(r.Next((Int32)_tileWidth, (Int32)_tileWidth * 5),
                                    r.Next((Int32)_tileHeight, (Int32)_tileHeight * 5));
                TestShapeType type = (TestShapeType)r.Next(0, (Int32)TestShapeType.Last);

                //Color color = HlsColor.ColorFromHLS((x * 240) / cols, 100, 240 - ((y * 240) / rows));
                TestShape shape = new TestShape(new Rect(pos, s), type, r);
                SetRandomBrushes(shape, r);
                Canvas.Graph.AddVirtualChild(shape);
                count--;
            }
        }

        private readonly String[] _colorNames = new String[10];
        private readonly Brush[] _strokeBrushes = new Brush[10];
        private readonly Brush[] _fillBrushes = new Brush[10];
        private readonly IStatusBar _statusbar;
        public IVirtualCanvas Canvas { get; private set; }

        void SetRandomBrushes(TestShape s, Random r)
        {
            Int32 i = r.Next(0, 10);
            if (_strokeBrushes[i] == null)
            {
                Color color = Color.FromRgb((Byte)r.Next(0, 255), (Byte)r.Next(0, 255), (Byte)r.Next(0, 255));
                HlsColor hls = new HlsColor(color);
                Color c1 = hls.Darker(0.25f);
                Color c2 = hls.Lighter(0.25f);
                Brush fill = new LinearGradientBrush(Color.FromArgb(0x80, c1.R, c1.G, c1.B),
                    Color.FromArgb(0x80, color.R, color.G, color.B), 45);
                Brush stroke = new LinearGradientBrush(Color.FromArgb(0x80, color.R, color.G, color.B),
                    Color.FromArgb(0x80, c2.R, c2.G, c2.B), 45);

                _colorNames[i] = "#" + color.R.ToString("X2", CultureInfo.InvariantCulture) +
                    color.G.ToString("X2", CultureInfo.InvariantCulture) +
                    color.B.ToString("X2", CultureInfo.InvariantCulture);
                _strokeBrushes[i] = stroke;
                _fillBrushes[i] = fill;
            }

            s.Label = _colorNames[i];
            s.Stroke = _strokeBrushes[i];
            s.Fill = _fillBrushes[i];
        }

        void OnScaleChanged(Object sender, EventArgs e)
        {
            // Make the grid lines get thinner as you zoom in
            Double t = _gridLines.StrokeThickness = 0.1 / Canvas.Graph.Scale.ScaleX;
            Canvas.Graph.Backdrop.BorderThickness = new Thickness(t);
        }

        private readonly Int32 lastTick = Environment.TickCount;
        private readonly Int32 addedPerSecond = 0;
        private readonly Int32 removedPerSecond = 0;

        void OnVisualsChanged(Object sender, VisualChangeEventArgs e)
        {
            //if (_animateStatus)
            //{
            //    StatusText.Text = string.Format(CultureInfo.InvariantCulture, "{0} live visuals of {1} total", grid.LiveVisualCount, _totalVisuals);

            //    int tick = Environment.TickCount;
            //    if (e.Added != 0 || e.Removed != 0)
            //    {
            //        addedPerSecond += e.Added;
            //        removedPerSecond += e.Removed;
            //        if (tick > lastTick + 100)
            //        {
            //            Created.BeginAnimation(Rectangle.WidthProperty, new DoubleAnimation(
            //                Math.Min(addedPerSecond, 450),
            //                new Duration(TimeSpan.FromMilliseconds(100))));
            //            CreatedLabel.Text = addedPerSecond.ToString(CultureInfo.InvariantCulture) + " created";
            //            addedPerSecond = 0;

            //            Destroyed.BeginAnimation(Rectangle.WidthProperty, new DoubleAnimation(
            //                Math.Min(removedPerSecond, 450),
            //                new Duration(TimeSpan.FromMilliseconds(100))));
            //            DestroyedLabel.Text = removedPerSecond.ToString(CultureInfo.InvariantCulture) + " disposed";
            //            removedPerSecond = 0;
            //        }
            //    }
            //    if (tick > lastTick + 1000)
            //    {
            //        lastTick = tick;
            //    }
            //}
        }

        void OnAnimateStatus(Object sender, RoutedEventArgs e)
        {
            //MenuItem item = (MenuItem)sender;
            //_animateStatus = item.IsChecked = !item.IsChecked;

            //StatusText.Text = "";
            //Created.BeginAnimation(Rectangle.WidthProperty, null);
            //Created.Width = 0;
            //CreatedLabel.Text = "";
            //Destroyed.BeginAnimation(Rectangle.WidthProperty, null);
            //Destroyed.Width = 0;
            //DestroyedLabel.Text = "";
        }

        public Boolean ShowGridLines
        {
            get => _showGridLines;
            set
            {
                _showGridLines = value;
                if (value)
                {
                    Double width = _tileWidth + _tileMargin;
                    Double height = _tileHeight + _tileMargin;

                    Double numTileToAccumulate = 16;

                    Polyline gridCell = _gridLines;
                    gridCell.Margin = new Thickness(_tileMargin);
                    gridCell.Stroke = Brushes.Blue;
                    gridCell.StrokeThickness = 0.1;
                    gridCell.Points = new PointCollection(new Point[] { new Point(0, height-0.1),
                        new Point(width-0.1, height-0.1), new Point(width-0.1, 0) });
                    VisualBrush gridLines = new VisualBrush(gridCell)
                    {
                        TileMode = TileMode.Tile,
                        Viewport = new Rect(0, 0, 1.0 / numTileToAccumulate, 1.0 / numTileToAccumulate),
                        AlignmentX = AlignmentX.Center,
                        AlignmentY = AlignmentY.Center
                    };

                    VisualBrush outerVB = new VisualBrush();
                    Rectangle outerRect = new Rectangle
                    {
                        Width = 10.0,  //can be any size
                        Height = 10.0,
                        Fill = gridLines
                    };
                    outerVB.Visual = outerRect;
                    outerVB.Viewport = new Rect(0, 0,
                        width * numTileToAccumulate, height * numTileToAccumulate);
                    outerVB.ViewportUnits = BrushMappingMode.Absolute;
                    outerVB.TileMode = TileMode.Tile;

                    Canvas.Graph.Backdrop.Background = outerVB;

                    Border border = Canvas.Graph.Backdrop;
                    border.BorderBrush = Brushes.Blue;
                    border.BorderThickness = new Thickness(0.1);
                    Canvas.Graph.InvalidateVisual();
                }
                else
                {
                    Canvas.Graph.Backdrop.Background = null;
                }
            }
        }
    }
}
