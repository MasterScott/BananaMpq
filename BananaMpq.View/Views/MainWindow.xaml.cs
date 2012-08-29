﻿using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using BananaMpq.View.Infrastructure;
using BananaMpq.View.Presenters;

namespace BananaMpq.View.Views
{
    public partial class MainWindow : Window, ISceneView
    {
        private readonly ScenePresenter _scenePresenter;
        private readonly AdtChunkHierachyPresenter _adtPresenter;
        private readonly LoadedWmoChunkHierarchyPresenter _wmoPresenter;
        private readonly TileSelectionPresenter _tileSelectionPresenter;
        private readonly CameraChassis _chassis;
        private Point _pinnedMousePosition;

        public MainWindow()
        {
            InitializeComponent();
            _chassis = new CameraChassis();
            _adtPresenter = new AdtChunkHierachyPresenter(_adtView);
            _wmoPresenter = new LoadedWmoChunkHierarchyPresenter(_wmoView); 
            _tileSelectionPresenter = new TileSelectionPresenter();
            _scenePresenter = new ScenePresenter(this, _chassis);

            WireUpEventHandlers();
        }

        private void WireUpEventHandlers()
        {
            CompositionTarget.Rendering += RenderScene;
            Closed += (s, e) => CompositionTarget.Rendering -= RenderScene;
            Loaded += InitializeScene;
            KeyUp += (sender, args) => _chassis.HandleKey(args);
            KeyDown += (sender, args) => _chassis.HandleKey(args);
            MouseMove += OnMouseMove;
            MouseRightButtonDown += OnRightButtonDown;
            MouseRightButtonUp += OnRightButtonUp;
            _image.SizeChanged += (sender, args) => _scenePresenter.Resize((int)args.NewSize.Width, (int)args.NewSize.Height);
            
            _btnNew.Click += (sender, args) => OpenTileSelectionDialog();
        }

        private void RenderScene(object sender, EventArgs args)
        {
            _imageSource.Lock();
            _scenePresenter.Render((args as RenderingEventArgs).RenderingTime);
            _imageSource.Unlock();
        }

        public void OpenTileSelectionDialog()
        {
            _tileSelectionPresenter.ConductModal(new TileSelectionDialog { Owner = this, ShowInTaskbar = false });
        }

        private void OnRightButtonUp(object sender, MouseButtonEventArgs args)
        {
            Infrastructure.Cursor.Show = true;
            Infrastructure.Cursor.Position = _pinnedMousePosition;
        }

        private void OnRightButtonDown(object sender, MouseButtonEventArgs args)
        {
            Infrastructure.Cursor.Show = false;
            _pinnedMousePosition = Infrastructure.Cursor.Position;
        }

        private void OnMouseMove(object sender, MouseEventArgs args)
        {
            if (args.RightButton == MouseButtonState.Pressed)
            {
                var delta = Infrastructure.Cursor.Position - _pinnedMousePosition;
                if (delta.X != 0 || delta.Y != 0)
                {
                    Infrastructure.Cursor.Show = false;
                    Infrastructure.Cursor.Position = _pinnedMousePosition;
                    _chassis.HandleInputDelta(delta);
                }
            }
            else
            {
                Infrastructure.Cursor.Show = true;
            }
        }

        private void InitializeScene(object sender, EventArgs e)
        {
            _scenePresenter.HandleDeviceReset();
            _scenePresenter.ParseCommandLine(Environment.GetCommandLineArgs());
        }

        #region Implementation of ISceneView

        public IntPtr WindowHandle
        {
            get { return (PresentationSource.FromVisual(this) as HwndSource).Handle; }
        }

        public IDisposable StartRenderPass(IntPtr surfacePointer)
        {
            _imageSource.Lock();
            _imageSource.SetBackBuffer(D3DResourceType.IDirect3DSurface9, surfacePointer);
            return new ImageUnlocker(_imageSource);
        }

        private class ImageUnlocker : IDisposable
        {
            private readonly D3DImage _image;

            public ImageUnlocker(D3DImage image)
            {
                _image = image;
            }

            #region Implementation of IDisposable

            public void Dispose()
            {
                _image.AddDirtyRect(new Int32Rect(0, 0, _image.PixelWidth, _image.PixelHeight));
                _image.Unlock();
            }

            #endregion
        }

        #endregion
    }
}
