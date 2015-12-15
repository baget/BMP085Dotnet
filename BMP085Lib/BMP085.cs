/*********************************************************************
** BMP085 (I2C) .Net
** an Universal (UWP) .NET library 
**
** Tested on Rasbperry Pi 2 With Windows 10 IoT
**
**	Author: Oren Weil 
**	
**	The MIT License (MIT)
**	Copyright (c) 2015 Oren Weil
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
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

namespace org.baget.BMP085Lib
{
    public class BMP085 : IDisposable
    {
        /// <summary>
        /// Default I2C Address
        /// </summary>
        public const int DEFAULT_ADDR = 0x77;

        /// <summary>
        /// REGISTERS Address
        /// </summary>
        private enum BMP085_REGISTER : byte
        {
            BMP085_REGISTER_CAL_AC1 = 0xAA,  // R   Calibration data (16 bits)
            BMP085_REGISTER_CAL_AC2 = 0xAC,  // R   Calibration data (16 bits)
            BMP085_REGISTER_CAL_AC3 = 0xAE,  // R   Calibration data (16 bits)
            BMP085_REGISTER_CAL_AC4 = 0xB0,  // R   Calibration data (16 bits)
            BMP085_REGISTER_CAL_AC5 = 0xB2,  // R   Calibration data (16 bits)
            BMP085_REGISTER_CAL_AC6 = 0xB4,  // R   Calibration data (16 bits)
            BMP085_REGISTER_CAL_B1 = 0xB6,  // R   Calibration data (16 bits)
            BMP085_REGISTER_CAL_B2 = 0xB8,  // R   Calibration data (16 bits)
            BMP085_REGISTER_CAL_MB = 0xBA,  // R   Calibration data (16 bits)
            BMP085_REGISTER_CAL_MC = 0xBC,  // R   Calibration data (16 bits)
            BMP085_REGISTER_CAL_MD = 0xBE,  // R   Calibration data (16 bits)
            BMP085_REGISTER_CHIPID = 0xD0,
            BMP085_REGISTER_VERSION = 0xD1,
            BMP085_REGISTER_SOFTRESET = 0xE0,
            BMP085_REGISTER_CONTROL = 0xF4,
            BMP085_REGISTER_TEMPDATA = 0xF6,
            BMP085_REGISTER_PRESSUREDATA = 0xF6,
            BMP085_REGISTER_READTEMPCMD = 0x2E,
            BMP085_REGISTER_READPRESSURECMD = 0x34
        };
        /*=========================================================================*/

        /// <summary>
        /// Working Mode
        /// </summary>
        public enum BMP085_MODE : byte
        {
            BMP085_MODE_ULTRALOWPOWER = 0,
            BMP085_MODE_STANDARD = 1,
            BMP085_MODE_HIGHRES = 2,
            BMP085_MODE_ULTRAHIGHRES = 3
        }

        /// <summary>
        /// CALIBRATION DATA
        /// </summary>
        private struct bmp085_calib_data
        {
            public Int16 ac1;
            public Int16 ac2;
            public Int16 ac3;
            public UInt16 ac4;
            public UInt16 ac5;
            public UInt16 ac6;
            public Int16 b1;
            public Int16 b2;
            public Int16 mb;
            public Int16 mc;
            public Int16 md;
        }


        private I2cDevice _i2cdevice;
        private bmp085_calib_data _bmp085_coeffs;


        /// <summary>
        /// I2C Address
        /// </summary>
        public int Address { get; set; }

        /// <summary>
        /// Working Mode
        /// </summary>
        public BMP085_MODE Mode { get; set; }


        /// <summary>
        /// Default Constructor, using high resolution mode and default address
        /// </summary>
        public BMP085() : this(DEFAULT_ADDR, BMP085_MODE.BMP085_MODE_ULTRAHIGHRES)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="addr">Device I2C Address</param>
        /// <param name="mode">Working Mode</param>
        public BMP085(int addr, BMP085_MODE mode)
        {
            Address = addr;
            Mode = mode;
        }

        /// <summary>
        /// Connect to Device
        /// </summary>
        public void Connect()
        {
            var settings = new I2cConnectionSettings(Address);
            settings.BusSpeed = I2cBusSpeed.FastMode;                       /* 400KHz bus speed */

            string aqs = I2cDevice.GetDeviceSelector();                     /* Get a selector string that will return all I2C controllers on the system */

            var task = Task.Run<I2cDevice>(async () =>
                {
                    var dis = await DeviceInformation.FindAllAsync(aqs);            /* Find the I2C bus controller devices with our selector string             */

                    var i2cdevice = await I2cDevice.FromIdAsync(dis[0].Id, settings);    /* Create an I2cDevice with our selected bus controller and I2C settings    */

                    if (i2cdevice == null)
                    {
                        throw new UnauthorizedAccessException(string.Format($"unable to access Slave address {settings.SlaveAddress} on I2C Controller {dis[0].Id} is currently in use."));
                    }

                    return i2cdevice;
                });

            _i2cdevice = task.Result;

            var chipId = read8(BMP085_REGISTER.BMP085_REGISTER_CHIPID);

            if (chipId != 0x55)
            {
                throw new InvalidOperationException("Wrong Chip ID");
            }

            readCoefficients();

        }

        /// <summary>
        /// Read Raw Temperature Value from the device
        /// </summary>
        /// <returns>Raw Temperature</returns>
        public Int32 readRawTemperature()
        {
            UInt16 t;

            writeCommand(BMP085_REGISTER.BMP085_REGISTER_CONTROL, (byte)BMP085_REGISTER.BMP085_REGISTER_READTEMPCMD);

            Task.Delay(5);

            t = read16(BMP085_REGISTER.BMP085_REGISTER_TEMPDATA);

            return t;
        }

        /// <summary>
        /// Read Raw Pressure Value from the device
        /// </summary>
        /// <returns>Raw Pressure</returns>
        public Int32 readRawPressure()
        {
            byte p8;
            UInt16 p16;
            Int32 p32;

            int ctl = (byte)(BMP085_REGISTER.BMP085_REGISTER_READPRESSURECMD) + ((byte)Mode) << 6;

            writeCommand(BMP085_REGISTER.BMP085_REGISTER_CONTROL, (byte)ctl);
            switch (Mode)
            {
                case BMP085_MODE.BMP085_MODE_ULTRALOWPOWER:
                    Task.Delay(5);
                    break;
                case BMP085_MODE.BMP085_MODE_STANDARD:
                    Task.Delay(8);
                    break;
                case BMP085_MODE.BMP085_MODE_HIGHRES:
                    Task.Delay(14);
                    break;
                case BMP085_MODE.BMP085_MODE_ULTRAHIGHRES:
                default:
                    Task.Delay(26);
                    break;
            }

            p16 = read16(BMP085_REGISTER.BMP085_REGISTER_PRESSUREDATA);
            p32 = (int)((UInt32)p16 << 8);
            p8 = read8(BMP085_REGISTER.BMP085_REGISTER_PRESSUREDATA + 2);
            p32 += p8;
            p32 >>= (8 - (byte)Mode);

            return p32;
        }

        /// <summary>
        ///  Reads the temperatures in degrees Celsius
        /// </summary>
        /// <returns>temperatures in degrees Celsius</returns>
        public decimal getTemperature()
        {
            Int32 UT, B5;     // following ds convention
            decimal t;

            UT = readRawTemperature();

            B5 = computeB5(UT);
            t = (B5 + 8) >> 4;
            t /= 10;

            return t;
        }

        /// <summary>
        /// Gets the compensated pressure level in kPa
        /// </summary>
        /// <returns>pressure level in kPa</returns>
        public decimal getPressure()
        {
            decimal compp = 0;
            Int32 ut = 0, up = 0;
            Int32 x1, x2, b5, b6, x3, b3, p;
            UInt32 b4, b7;

            /* Get the raw pressure and temperature values */
            ut = readRawTemperature();
            up = readRawPressure();

            /* Temperature compensation */
            b5 = computeB5(ut);

            /* Pressure compensation */
            b6 = b5 - 4000;
            x1 = (_bmp085_coeffs.b2 * ((b6 * b6) >> 12)) >> 11;
            x2 = (_bmp085_coeffs.ac2 * b6) >> 11;
            x3 = x1 + x2;
            b3 = (((((Int32)_bmp085_coeffs.ac1) * 4 + x3) << (int)Mode) + 2) >> 2;
            x1 = (_bmp085_coeffs.ac3 * b6) >> 13;
            x2 = (_bmp085_coeffs.b1 * ((b6 * b6) >> 12)) >> 16;
            x3 = ((x1 + x2) + 2) >> 2;
            b4 = (_bmp085_coeffs.ac4 * (UInt32)(x3 + 32768)) >> 15;
            b7 = (UInt32)((UInt32)(up - b3) * (50000 >> (int)Mode));

            if (b7 < 0x80000000)
            {
                p = (Int32)((b7 << 1) / b4);
            }
            else
            {
                p = (Int32)((b7 / b4) << 1);
            }

            x1 = (p >> 8) * (p >> 8);
            x1 = (x1 * 3038) >> 16;
            x2 = (-7357 * p) >> 16;
            compp = p + ((x1 + x2 + 3791) >> 4);

            /* Assign compensated pressure value */
            return compp;
        }


        /// <summary>
        /// Get Compute Altitude 
        /// </summary>
        /// <param name="sealevelPressure">Sea Level Pressure</param>
        /// <returns>Altitude in meters</returns>
        public decimal getAltitude(decimal sealevelPressure)
        {
            decimal altitude;

            decimal pressure = getPressure();

            altitude = (decimal)(44330 * (1.0 - Math.Pow((double)(pressure / sealevelPressure), 0.1903)));

            return altitude;
        }

        /// <summary>
        /// Get Sealevel Pressure
        /// </summary>
        /// <param name="altitude_meters">Altitude in meters</param>
        /// <returns>Sealevel Pressure</returns>
        public UInt32 getSealevelPressure(decimal altitude_meters)
        {
            decimal pressure = getPressure();
            return (UInt32)(pressure / (decimal)(Math.Pow(1.0 - (float)altitude_meters / 44330, 5.255)));
        }


        /// <summary>
        /// Compute B5 coefficient used in temperature & pressure calcs.
        /// </summary>
        /// <param name="ut"></param>
        /// <returns>B5 coefficient </returns>
        private Int32 computeB5(Int32 ut)
        {
            Int32 X1 = (ut - (Int32)_bmp085_coeffs.ac6) * ((Int32)_bmp085_coeffs.ac5) >> 15;
            Int32 X2 = ((Int32)_bmp085_coeffs.mc << 11) / (X1 + (Int32)_bmp085_coeffs.md);
            return X1 + X2;
        }

        #region Read and Write Private Functions
        private void writeCommand(BMP085_REGISTER reg, byte value)
        {
            byte[] writeBuf = new byte[] { (byte)reg, value };

            _i2cdevice.Write(writeBuf);
        }

        private byte read8(BMP085_REGISTER reg)
        {
            var readAddr = new byte[] { (byte)reg };
            var readbuf = new byte[sizeof(byte)];

            _i2cdevice.WriteRead(readAddr, readbuf);

            return readbuf[0];
        }

        private UInt16 read16(BMP085_REGISTER reg)
        {
            var readAddr = new byte[] { (byte)reg };
            var readbuf = new byte[sizeof(UInt16)];

            _i2cdevice.WriteRead(readAddr, readbuf);

            return BitConverter.ToUInt16(readbuf.Reverse().ToArray(), 0);
        }

        private Int16 readS16(BMP085_REGISTER reg)
        {
            var readAddr = new byte[] { (byte)reg };
            var readbuf = new byte[sizeof(Int16)];

            _i2cdevice.WriteRead(readAddr, readbuf);

            return BitConverter.ToInt16(readbuf.Reverse().ToArray(), 0);
        }

        private void readCoefficients()
        {
            _bmp085_coeffs.ac1 = readS16(BMP085_REGISTER.BMP085_REGISTER_CAL_AC1);
            _bmp085_coeffs.ac2 = readS16(BMP085_REGISTER.BMP085_REGISTER_CAL_AC2);
            _bmp085_coeffs.ac3 = readS16(BMP085_REGISTER.BMP085_REGISTER_CAL_AC3);
            _bmp085_coeffs.ac4 = read16(BMP085_REGISTER.BMP085_REGISTER_CAL_AC4);
            _bmp085_coeffs.ac5 = read16(BMP085_REGISTER.BMP085_REGISTER_CAL_AC5);
            _bmp085_coeffs.ac6 = read16(BMP085_REGISTER.BMP085_REGISTER_CAL_AC6);
            _bmp085_coeffs.b1 = readS16(BMP085_REGISTER.BMP085_REGISTER_CAL_B1);
            _bmp085_coeffs.b2 = readS16(BMP085_REGISTER.BMP085_REGISTER_CAL_B2);
            _bmp085_coeffs.mb = readS16(BMP085_REGISTER.BMP085_REGISTER_CAL_MB);
            _bmp085_coeffs.mc = readS16(BMP085_REGISTER.BMP085_REGISTER_CAL_MC);
            _bmp085_coeffs.md = readS16(BMP085_REGISTER.BMP085_REGISTER_CAL_MD);
        }

        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _i2cdevice?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion


    }
}
