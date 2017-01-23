//
// Copyright (c) 2017 Equine Smart Bits, LLC. All rights reserved

using System;
using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using Android.OS;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Xamarin.Android;
using Android.Content.PM;
using Android.Graphics;
using Android.Hardware.Usb;
using Android.Util;
using System.Linq;
using System.Text;
using System.Threading;
using Hoho.Android.UsbSerial.Driver;
using Hoho.Android.UsbSerial.Util;
using OxyPlot.Axes;

namespace ESB
{
    [Activity(Label = "@string/app_name", LaunchMode = LaunchMode.SingleTop)]
    public class ChartViewActivity : Activity
    {
        static readonly string TAG = typeof(ChartViewActivity).Name;

        public const string EXTRA_TAG = "PortInfo";

        private PlotView plotViewModel;
        private LinearLayout mLLayoutModel;
        public PlotModel MyModel { get; set; }

        UsbManager usbManager;
        IUsbSerialPort port;

        string input_line;

        SerialInputOutputManager serialIoManager;

        private int[] modelAllocValues = new int[] { 12, 5, 2, 40, 40, 1 };
        private string[] modelAllocations = new string[] { "Slice1", "Slice2", "Slice3", "Slice4", "Slice5", "Slice6" };
        string[] colors = new string[] { "#7DA137", "#6EA6F3", "#999999", "#3B8DA5", "#F0BA22", "#EC8542" };
        int total = 0;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.ChartView);

            usbManager = GetSystemService(Context.UsbService) as UsbManager;

            plotViewModel = FindViewById<PlotView>(Resource.Id.plotViewModel);
            mLLayoutModel = FindViewById<LinearLayout>(Resource.Id.linearLayoutModel);

            var plotModel1 = new PlotModel();

            plotModel1.PlotMargins = new OxyThickness(40, 40, 40, 40);
            var linearAxis1 = new LinearAxis();
            linearAxis1.MajorGridlineStyle = LineStyle.Solid;
            linearAxis1.MinorGridlineStyle = LineStyle.Dot;
            linearAxis1.Title = "HR";
            linearAxis1.Key = "HR";
            linearAxis1.Position = AxisPosition.Left;
            plotModel1.Axes.Add(linearAxis1);
            var linearAxis2 = new LinearAxis();
            linearAxis2.MajorGridlineStyle = LineStyle.Solid;
            linearAxis2.MinorGridlineStyle = LineStyle.Dot;
            linearAxis2.Position = AxisPosition.Right;
            linearAxis2.Title = "%SpO2";
            linearAxis2.Key = "SP";
            plotModel1.Axes.Add(linearAxis2);

            var seriesHR = new LineSeries()
            {
                Color = OxyColors.SkyBlue,
                MarkerType = MarkerType.Circle,
                MarkerSize = 6,
                MarkerStroke = OxyColors.White,
                MarkerFill = OxyColors.SkyBlue,
                YAxisKey = "HR",
                MarkerStrokeThickness = 1.5
            };

            seriesHR.Points.Add(new DataPoint(0, 10));
            seriesHR.Points.Add(new DataPoint(10, 40));
            seriesHR.Points.Add(new DataPoint(40, 20));
            seriesHR.Points.Add(new DataPoint(60, 30));
            plotModel1.Series.Add(seriesHR);

            MyModel = plotModel1;
            plotViewModel.Model = MyModel;
        }

        protected override void OnPause()
        {
            Log.Info(TAG, "OnPause");

            base.OnPause();

            if (serialIoManager != null && serialIoManager.IsOpen)
            {
                Log.Info(TAG, "Stopping IO manager ..");
                try
                {
                    serialIoManager.Close();
                }
                catch (Java.IO.IOException)
                {
                    // ignore
                }
            }
        }

        protected async override void OnResume()
        {
            Log.Info(TAG, "OnResume");

            base.OnResume();

            input_line = "";

            var portInfo = Intent.GetParcelableExtra(EXTRA_TAG) as UsbSerialPortInfo;
            int vendorId = portInfo.VendorId;
            int deviceId = portInfo.DeviceId;
            int portNumber = portInfo.PortNumber;

            Log.Info(TAG, string.Format("VendorId: {0} DeviceId: {1} PortNumber: {2}", vendorId, deviceId, portNumber));

            var drivers = await MainActivity.FindAllDriversAsync(usbManager);
            var driver = drivers.Where((d) => d.Device.VendorId == vendorId && d.Device.DeviceId == deviceId).FirstOrDefault();
            if (driver == null)
                throw new Exception("Driver specified in extra tag not found.");

            port = driver.Ports[portNumber];
            if (port == null)
            {
                // hrTextView.Text = "No serial device.";
                return;
            }
            Log.Info(TAG, "port=" + port);

            // hrTextView.Text = "Serial device: " + port.GetType().Name;

            serialIoManager = new SerialInputOutputManager(port)
            {
                BaudRate = 9600,
                DataBits = 8,
                StopBits = StopBits.One,
                Parity = Parity.None,
            };
            serialIoManager.DataReceived += (sender, e) => {
                RunOnUiThread(() => {
                    UpdateReceivedData(e.Data);
                });
            };
            serialIoManager.ErrorReceived += (sender, e) => {
                RunOnUiThread(() => {
                    var intent = new Intent(this, typeof(MainActivity));
                    StartActivity(intent);
                });
            };

            Log.Info(TAG, "Starting IO manager ..");
            try
            {
                serialIoManager.Open(usbManager);
                Thread.Sleep(2000);
                byte[] cmd = Encoding.ASCII.GetBytes("  ");
                port.Write(cmd, 1000);
                Thread.Sleep(1000);
            }
            catch (Java.IO.IOException e)
            {
                // hrTextView.Text = "Error opening device: " + e.Message;
                return;
            }
        }

        void UpdateReceivedData(byte[] data)
        {
            string result = System.Text.Encoding.UTF8.GetString(data);

            input_line += result;

            int count = result.Length;

            if (!result.EndsWith("\n"))
                return;

            string line = input_line;

            input_line = "";

            int hr, sp;
            double temp;
            bool calculated;

            ParseLog.GetData(line, out hr, out sp, out temp, out calculated);

            if (temp > 0.0)
            {
                // tempTextView.Text = "Temp = " + temp.ToString() + "F";
            }
            else if (calculated)
            {
                // hrTextView.Text = "HR = " + hr.ToString() + " bpm";
                // spTextView.Text = "SP = " + sp.ToString() + "%";
            }
        }
    }
}
