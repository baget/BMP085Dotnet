/*********************************************************************
** BMP085 (I2C) .Net
** an Universal (UWP) .NET library 
**
** Tested on Rasbperry Pi 2 With Windows 10 IoT
**
**	Author: Oren Weil 
**	
**	The MIT License (MIT)
**	Permission is hereby granted, free of charge, to any person obtaining a copy
**	of this software and associated documentation files (the "Software"), to deal
**	in the Software without restriction, including without limitation the rights
**	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
**	copies of the Software, and to permit persons to whom the Software is
**	furnished to do so, subject to the following conditions:
**	The above copyright notice and this permission notice shall be included in all
**	copies or substantial portions of the Software.
**	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
**	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
**	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
**	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
**	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
**	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
**	SOFTWARE.
**
** 
**  based on Adafruit BMP085 Arduino Lib
**    https://github.com/adafruit/Adafruit_BMP085_Unified/
**  Adafruit lib was Written by Kevin Townsend for Adafruit Industries.   
**  BSD license, all text above must be included in any redistribution 
************************************************************************/

using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using org.baget.BMP085Lib;

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
