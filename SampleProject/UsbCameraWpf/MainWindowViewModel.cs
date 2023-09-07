using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using GitHub.secile.Video;

namespace UsbCameraWpf
{
    class MainWindowViewModel : ViewModel
    {
        private BitmapSource _Preview;
        public BitmapSource Preview
        {
            get { return _Preview; }
            set
            {
                if (_Preview != value)
                    _Preview = value;
                OnPropertyChanged();
            }
        }

        private BitmapSource _Capture;
        public BitmapSource Capture
        {
            get { return _Capture; }
            set
            {
                _Capture = value;
                OnPropertyChanged();
            }
        }
        private bool isWBManual;

        public bool IsWBManual
        {
            get { return isWBManual; }
            set
            {
                isWBManual = value;
                prop = Cam.Properties[DirectShow.VideoProcAmpProperty.WhiteBalance];
                if (prop.Available && prop.CanAuto)
                {
                    var currentval = prop.GetValue();
                    if (value)
                    {
#if DEBUG
                        currentval = 5500;
#endif
                        Debug.WriteLine("Manual WB: " + currentval.ToString());
                        prop.SetValue(DirectShow.CameraControlFlags.Manual, currentval);
                    }
                    else
                    {
                        prop.SetValue(DirectShow.CameraControlFlags.Auto, 1);
                        var autoval = prop.GetValue();
                        Debug.WriteLine("WB Value: " + autoval.ToString());
                    }
                }
                OnPropertyChanged();
            }
        }

        public ICommand GetBitmap { get; private set; }

        public ICommand GetStillImage { get; private set; }
        public ICommand Increase1Brightness { get; private set; }
        public ICommand Decrease1Brightness { get; private set; }

        public ICommand IncreaseContrast { get; private set; }
        public ICommand DecreaseContrast { get; private set; }
        public ICommand IncreaseSaturation { get; private set; }
        public ICommand DecreaseSaturation { get; private set; }

        public ICommand IncreaseBacklightCompensation { get; private set; }
        public ICommand DecreaseBacklightCompensation { get; private set; }
        private UsbCamera Cam;

        public MainWindowViewModel()
        {
            // find device.
            var devices = UsbCamera.FindDevices();
            if (devices.Length == 0) return; // no device.

            // get video format.
            var cameraIndex = 0;
            var formats = UsbCamera.GetVideoFormat(cameraIndex);

            #region Initialization
            // select the format you want.
            foreach (var item in formats) Console.WriteLine(item);
            var format = formats[0];

            // create instance.
            var camera = new UsbCamera(cameraIndex, format);
            Cam = camera;
            // to show preview, there are 3 ways.
            // 1. subscribe PreviewCaptured. (recommended.)
            camera.PreviewCaptured += (bmp) =>
            {
                // passed image can only be used for preview with data binding.
                // the image is single instance and updated by library. DO NOT USE for other purpose.
                Preview = bmp;
            };

            // 2. use Timer and GetBitmap().
            //var timer = new System.Windows.Threading.DispatcherTimer();
            //timer.Interval = TimeSpan.FromMilliseconds(1000.0 / 30);
            //timer.Tick += (s, ev) => Preview = camera.GetBitmap();
            //timer.Start();

            // 3. use SetPreviewControl and WindowsFormshost. (works light, but you can't use WPF merit.)
            // SetPreviewControl requires window handle but WPF control does not have handle.
            // it is recommended to use PictureBox with WindowsFormsHost.
            // or use handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            //var handle = pictureBox.Handle passed from MainWindow.xaml.
            //camera.SetPreviewControl(handle, new System.Windows.Size(320, 240));

            // start.
            camera.Start();

            GetBitmap = new RelayCommand(() => Capture = camera.GetBitmap());

            if (camera.StillImageAvailable)
            {
                GetStillImage = new RelayCommand(() => camera.StillImageTrigger());
                camera.StillImageCaptured += bmp => Capture = bmp;
            }

            #endregion

            #region RelayCommands
            Increase1Brightness = new RelayCommand(() =>
                {
                    prop = camera.Properties[DirectShow.VideoProcAmpProperty.Brightness];
                    IncreaseValue(prop);
                });
            Decrease1Brightness = new RelayCommand(() =>
            {
                prop = camera.Properties[DirectShow.VideoProcAmpProperty.Brightness];
                DecreaseValue(prop);
            });
            IncreaseContrast = new RelayCommand(() =>
            {
                prop = camera.Properties[DirectShow.VideoProcAmpProperty.Contrast];
                IncreaseValue(prop);
            });
            DecreaseContrast = new RelayCommand(() =>
            {
                prop = camera.Properties[DirectShow.VideoProcAmpProperty.Contrast];
                DecreaseValue(prop);
            });
            IncreaseSaturation = new RelayCommand(() =>
            {
                prop = camera.Properties[DirectShow.VideoProcAmpProperty.Saturation];
                IncreaseValue(prop);
            });
            DecreaseSaturation = new RelayCommand(() =>
            {
                prop = camera.Properties[DirectShow.VideoProcAmpProperty.Saturation];
                DecreaseValue(prop);
            });
            IncreaseBacklightCompensation = new RelayCommand(() =>
            {
                prop = camera.Properties[DirectShow.VideoProcAmpProperty.BacklightCompensation];
                IncreaseValue(prop);
            });
            DecreaseBacklightCompensation = new RelayCommand(() =>
            {
                prop = camera.Properties[DirectShow.VideoProcAmpProperty.BacklightCompensation];
                DecreaseValue(prop);
            });
            #endregion
        }

        private void IncreaseValue(UsbCamera.PropertyItems.Property prop)
        {
            if (prop.Available)
            {
                int val = prop.GetValue();
                var min = prop.Min;
                var max = prop.Max;
                var def = prop.Default;
                var step = prop.Step;
                if (val < max) { val = val + step; }

                prop.SetValue(DirectShow.CameraControlFlags.Manual, val);
                val = prop.GetValue();
                Debug.WriteLine("New val is: " + val);
            }
            else
            {
                Debug.WriteLine("No Saturation? ");

            }
        }

        private void DecreaseValue(UsbCamera.PropertyItems.Property prop)
        {
            if (prop.Available)
            {
                int val = prop.GetValue();
                var min = prop.Min;
                var max = prop.Max;
                var def = prop.Default;
                var step = prop.Step;
                if (val > min) { val--; }

                prop.SetValue(DirectShow.CameraControlFlags.Manual, val);
                val = prop.GetValue();
                Debug.WriteLine("New val is: " + val);
            }
            else
            {
                Debug.WriteLine("No Saturation? ");

            }
        }

        UsbCamera.PropertyItems.Property prop;
    }
}
