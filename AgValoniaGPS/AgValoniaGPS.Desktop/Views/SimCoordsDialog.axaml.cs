using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace AgValoniaGPS.Desktop.Views
{
    public partial class SimCoordsDialog : Window
    {
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public bool DialogResult { get; private set; }

        public SimCoordsDialog()
        {
            InitializeComponent();
        }

        public SimCoordsDialog(double currentLatitude, double currentLongitude)
        {
            InitializeComponent();

            // Set current values
            LatitudeInput.Value = (decimal)currentLatitude;
            LongitudeInput.Value = (decimal)currentLongitude;
        }

        private void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            // Get values from inputs
            Latitude = (double)(LatitudeInput.Value ?? 0);
            Longitude = (double)(LongitudeInput.Value ?? 0);
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
