using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using org.baget.BMP085Lib;
using System.Threading.Tasks;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BMP085TestApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private BMP085 _bmp;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
 

        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            using (_bmp = new BMP085())
            {
                try
                {
                    _bmp.Connect();                    

                    var temp = _bmp.getTemperature();
                    var pressure = _bmp.getPressure();
                    var alt = _bmp.getAltitude(10);
                    var seal = _bmp.getSealevelPressure(10);

                    txtTemp.Text = temp.ToString();
                    txtHumi.Text = pressure.ToString();
                    
                }
                catch (Exception ex)
                {
                    Windows.UI.Popups.MessageDialog dlg = new Windows.UI.Popups.MessageDialog(ex.Message, "Error!");
                }


            }
        }
    }
}
