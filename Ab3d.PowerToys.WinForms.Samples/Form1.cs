using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Media3D;
using Ab3d.Cameras;
using Ab3d.Common.Cameras;
using Ab3d.Common.EventManager3D;
using Ab3d.Controls;
using Ab3d.Utilities;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using Point = System.Drawing.Point;

namespace Ab3d.PowerToys.WinForms.Samples
{
    public partial class Form1 : Form
    {
        // This sample shows how easy is to use WPF inside WinForms application.
        // It also shows how easy is to create interesting 3D scene with Ab3d.PowerToys library.

        private Viewport3D _viewport3D;
        private TargetPositionCamera _targetPositionCamera;
        private MouseCameraController _mouseCameraController;
        private Grid _rootGrid;
        private EventManager3D _eventManager3D;

        private bool _isSelectedBoxClicked;

        private double _totalClickedHeight;

        private DiffuseMaterial _normalMaterial = new DiffuseMaterial(System.Windows.Media.Brushes.Silver);
        private DiffuseMaterial _selectedMaterial = new DiffuseMaterial(System.Windows.Media.Brushes.Orange);
        private DiffuseMaterial _clickedMaterial = new DiffuseMaterial(System.Windows.Media.Brushes.Red);

        public Form1()
        {
            InitializeComponent();

            SetUpWpf3D();
            Setup3DObjects();

            // HACK HACK HACK:
            // To pass mouse wheel events to WPF controls, we need to manually pass the mouse wheel event to WPF
            // The following class does that for us:
            MouseWheelMessageFilter.RegisterMouseWheelHandling(_rootGrid);
        }


        private void Setup3DObjects()
        {
            // The event manager will be used to manage the mouse events on our boxes
            _eventManager3D = new Ab3d.Utilities.EventManager3D(_viewport3D);


            // Add a wire grid
            var wireGridVisual3D = new Ab3d.Visuals.WireGridVisual3D()
            {
                Size = new System.Windows.Size(1000, 1000),
                HeightCellsCount = 10,
                WidthCellsCount = 10,
                LineThickness = 3
            };

            _viewport3D.Children.Add(wireGridVisual3D);


            // Create 7 x 7 boxes with different height
            for (int y = -3; y <= 3; y++)
            {
                for (int x = -3; x <= 3; x ++)
                {
                    // Height is based on the distance from the center
                    double height = (5 - Math.Sqrt(x * x + y * y)) * 60;

                    // Create the 3D Box visual element
                    var boxVisual3D = new Ab3d.Visuals.BoxVisual3D()
                    {
                        CenterPosition = new Point3D(x * 100, height / 2, y * 100),
                        Size = new Size3D(80, height, 80),
                        Material = _normalMaterial
                    };

                    _viewport3D.Children.Add(boxVisual3D);


                    // With EventManager we can subscribe to mouse events as we would have standard 2D controls:
                    var visualEventSource3D = new VisualEventSource3D(boxVisual3D);
                    visualEventSource3D.MouseEnter += BoxOnMouseEnter;
                    visualEventSource3D.MouseLeave += BoxOnMouseLeave;
                    visualEventSource3D.MouseClick += BoxOnMouseClick;

                    _eventManager3D.RegisterEventSource3D(visualEventSource3D);
                }
            }

            ToggleCameraAnimation(); // Start camer animation
        }

        private void BoxOnMouseClick(object sender, MouseButton3DEventArgs mouseButton3DEventArgs)
        {
            // HitObject is our BoxVisual3D
            var boxVisual3D = mouseButton3DEventArgs.HitObject as Ab3d.Visuals.BoxVisual3D;
            if (boxVisual3D == null)
                return; // This should not happen

            // Toggle clicked and normal material
            if (!_isSelectedBoxClicked)
            {
                boxVisual3D.Material = _clickedMaterial;
                _isSelectedBoxClicked = true;

                _totalClickedHeight += boxVisual3D.Size.Y;
            }
            else
            {
                boxVisual3D.Material = _normalMaterial;
                _isSelectedBoxClicked = false;

                _totalClickedHeight -= boxVisual3D.Size.Y;
            }

            UpdateTotalClickedHeightText();
        }

        private void BoxOnMouseEnter(object sender, Mouse3DEventArgs mouse3DEventArgs)
        {
            var boxVisual3D = mouse3DEventArgs.HitObject as Ab3d.Visuals.BoxVisual3D;
            if (boxVisual3D == null)
                return; // This should not happen

            // Set _isSelectedBoxClicked to true if the selected box is clicked (red) - this will be used on MouseLeave
            _isSelectedBoxClicked = ReferenceEquals(boxVisual3D.Material, _clickedMaterial);

            boxVisual3D.Material = _selectedMaterial;
        }

        private void BoxOnMouseLeave(object sender, Mouse3DEventArgs mouse3DEventArgs)
        {
            var boxVisual3D = mouse3DEventArgs.HitObject as Ab3d.Visuals.BoxVisual3D;
            if (boxVisual3D == null)
                return; // This should not happen

            if (_isSelectedBoxClicked)
                boxVisual3D.Material = _clickedMaterial;
            else
                boxVisual3D.Material = _normalMaterial;
        }

        private void UpdateTotalClickedHeightText()
        {
            textBox1.Text = string.Format("Total clicked height: {0:0}\r\n{1}", _totalClickedHeight, textBox1.Text);
        }


        private void SetUpWpf3D()
        {
            // The following controls are usually defined in XAML in Wpf project
            // But here we can also define them in code.

            // We need a root grid because we will host more than one control
            _rootGrid = new Grid();
            _rootGrid.Background = System.Windows.Media.Brushes.White;


            // Viewport3D is a WPF control that can show 3D graphics
            _viewport3D = new Viewport3D();
            _rootGrid.Children.Add(_viewport3D);


            // Specify TargetPositionCamera that will show our 3D scene

            //<cameras:TargetPositionCamera Name="Camera1"
            //                    Heading="30" Attitude="-20" Bank="0" 
            //                    Distance="1300" TargetPosition="0 0 0" 
            //                    ShowCameraLight="Always"
            //                    TargetViewport3D="{Binding ElementName=MainViewport}"/>

            _targetPositionCamera = new Ab3d.Cameras.TargetPositionCamera()
            {
                TargetPosition = new Point3D(0, 0, 0),
                Distance = 1300,
                Heading = 30,
                Attitude = -20,
                ShowCameraLight = ShowCameraLightType.Always,
                TargetViewport3D = _viewport3D
            };

            _rootGrid.Children.Add(_targetPositionCamera);


            // Set rotate to right mouse button
            // and move to CRTL + right mouse button
            // Left mouse button is left for clicking on the 3D objects

            //<controls:MouseCameraController Name="MouseCameraController1"
            //                                RotateCameraConditions="RightMouseButtonPressed"
            //                                MoveCameraConditions="ControlKey, RightMouseButtonPressed" 
            //                                EventsSourceElement="{Binding ElementName=RootViewportBorder}"
            //                                TargetCamera="{Binding ElementName=Camera1}"/>

            _mouseCameraController = new Ab3d.Controls.MouseCameraController()
            {
                RotateCameraConditions = MouseCameraController.MouseAndKeyboardConditions.RightMouseButtonPressed,
                MoveCameraConditions = MouseCameraController.MouseAndKeyboardConditions.RightMouseButtonPressed | MouseCameraController.MouseAndKeyboardConditions.ControlKey,
                EventsSourceElement = _rootGrid,
                TargetCamera = _targetPositionCamera
            };

            _rootGrid.Children.Add(_mouseCameraController);


            // Show buttons that can be used to rotate and move the camera

            //<controls:CameraControlPanel VerticalAlignment="Bottom" HorizontalAlignment="Right" Margin="5" Width="225" Height="75" ShowMoveButtons="True"
            //                                TargetCamera="{Binding ElementName=Camera1}"/>

            var cameraControlPanel = new Ab3d.Controls.CameraControlPanel()
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(5, 5, 5, 5),
                Width = 225,
                Height = 75,
                ShowMoveButtons = true,
                TargetCamera = _targetPositionCamera
            };

            _rootGrid.Children.Add(cameraControlPanel);


            // Finally add the root WPF Grid to elementHost1
            elementHost1.Child = _rootGrid;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var form2 = new Form2();
            form2.Closed += (o, args) => this.Show();

            this.Hide();
            form2.Show();
        }

        private void animateButton_Click(object sender, EventArgs e)
        {
            ToggleCameraAnimation();
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            foreach (var boxVisual3D in _viewport3D.Children.OfType<Ab3d.Visuals.BoxVisual3D>())
                boxVisual3D.Material = _normalMaterial;

            _totalClickedHeight = 0;
            UpdateTotalClickedHeightText();
        }

        private void ToggleCameraAnimation()
        {
            if (_targetPositionCamera.IsRotating)
            {
                _targetPositionCamera.StopRotation();
                animateButton.Text = "Start animation";
            }
            else
            {
                _targetPositionCamera.StartRotation(10, 0); // animate the camera with changing heading for 10 degrees in one second
                animateButton.Text = "Stop animation";
            }
        }
    }
}
