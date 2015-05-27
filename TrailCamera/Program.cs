using System;
using System.Collections;
using System.Threading;
using System.Net;
using System.IO;
using System.Security.Cryptography;

using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Presentation.Shapes;
using Microsoft.SPOT.Touch;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GregHolliday;
using Gadgeteer.Modules.GHIElectronics;

using Json.NETMF;

namespace TrailCamera
{
    public partial class Program
    {
        public Configuration config;
        public int currentFile = 1;

        public GTM.GHIElectronics.TempHumidSI70.Measurement temp;

        #region Main Program
        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {
            Debug.Print("Program Started");
            errorStatusLED.TurnGreen();
            try
            {
                displayMessage("Reading Config File");

                if (verifySDCard())
                {
                    loadConfigFile();
                }
                
                displayMessage("Initializing Wireless Network");
                initializeWireless();

                displayMessage("Setting Interupts");
                camera.PictureCaptured += camera_PictureCaptured;
                motionSensor.Motion_Sensed += motionSensor_Motion_Sensed;
                scrollUp.ButtonPressed += scrollUp_ButtonPressed;
                scrollDown.ButtonPressed += scrollDown_ButtonPressed;
                Timer tempTimer = new Timer(new TimerCallback(tempTimer_Tick), null, 1000, 3600000);

                displayMessage("Setting Time");
                DateTime time = new DateTime();
                time = setLocalTimeWebAPI();
                Microsoft.SPOT.Hardware.Utility.SetLocalTime(time);




            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                errorStatusLED.BlinkRepeatedly(GT.Color.Red);
            }
        }
        #endregion

        #region Timer Event
        private void tempTimer_Tick(object state)
        {
            temp = tempHumid.TakeMeasurement();
            displayMessage(DateTime.Now.ToString(), "Temp: " + temp.TemperatureFahrenheit + " Hum: " + temp.RelativeHumidity);
        }
        #endregion

        #region Intitialize Network
        private void initializeWireless()
        {
            try
            {
                wifi.NetworkInterface.Open();
                wifi.NetworkSettings.EnableDhcp();
                wifi.NetworkSettings.EnableDynamicDns();
                wifi.NetworkSettings.EnableStaticIP("192.168.1.99","255.255.255.0","192.168.1.1");

                //Debug.Print("Setting DHCP");
                //wifi.UseDHCP();

                GHI.Networking.WiFiRS9110.NetworkParameters[] scanResults = wifi.NetworkInterface.Scan(config.ssid);

                

                if (scanResults != null)
                {
                    scanResults[0].Key = config.password;
                    scanResults[0].SecurityMode = GHI.Networking.WiFiRS9110.SecurityMode.Wpa2;
                    scanResults[0].NetworkType = GHI.Networking.WiFiRS9110.NetworkType.AccessPoint;
                    wifi.NetworkInterface.Join(scanResults[0]);
                    Debug.Print("Network joined");
                    Thread.Sleep(1250);

                    Microsoft.SPOT.Net.NetworkInformation.NetworkInterface settings = wifi.NetworkSettings;

                    Debug.Print("---------------------------------");
                    Debug.Print("MAC: " + ByteExtensions.ToHexString(settings.PhysicalAddress));
                    Debug.Print("IP Address: " + settings.IPAddress);
                    Debug.Print("DHCP Enabled: " + settings.IsDhcpEnabled);
                    Debug.Print("Subnet Mask: " + settings.SubnetMask);
                    Debug.Print("Gateway: " + settings.GatewayAddress);
                    Debug.Print("---------------------------------");
                }

                if (wifi.IsNetworkUp)
                {
                    networkStatusLED.TurnGreen();
                    Microsoft.SPOT.Net.NetworkInformation.NetworkInterface settings = wifi.NetworkSettings;

                    Debug.Print("---------------------------------");
                    Debug.Print("MAC: " + ByteExtensions.ToHexString(settings.PhysicalAddress));
                    Debug.Print("IP Address: " + settings.IPAddress);
                    Debug.Print("DHCP Enabled: " + settings.IsDhcpEnabled);
                    Debug.Print("Subnet Mask: " + settings.SubnetMask);
                    Debug.Print("Gateway: " + settings.GatewayAddress);
                    Debug.Print("---------------------------------");
                }
                else
                {
                    networkStatusLED.BlinkRepeatedly(GT.Color.Red);
                }

            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                throw ex;
            }



        }
        #endregion

        #region Set Local Time From Server
        private DateTime setLocalTimeWebAPI()
        {
            try
            {
                System.Net.WebRequest request2 = System.Net.WebRequest.Create("https://trailmonitorapi.azurewebsites.net/api/getdatetime/");
                System.Net.WebResponse response2 = request2.GetResponse();
                Debug.Print("StatusDescription: " + ((System.Net.HttpWebResponse)response2).StatusDescription);
                System.IO.Stream dataStream2 = response2.GetResponseStream();
                System.IO.StreamReader reader2 = new System.IO.StreamReader(dataStream2);
                string responseFromServer2 = reader2.ReadToEnd();
                Debug.Print("responseFromServer2: " + responseFromServer2);
                reader2.Close();
                response2.Close();


                string string2find3 = "LocalDate\":\"";
                string string2find4 = "\"";
                int begin = responseFromServer2.IndexOf(string2find3) + string2find3.Length;
                int end = responseFromServer2.IndexOf(string2find4, begin);
                string datetimeX = responseFromServer2.Substring(begin, end - begin);
                Debug.Print("DatTime from WebAPI is: " + datetimeX);

                string year = datetimeX.Substring(0, 4);
                string month = datetimeX.Substring(5, 2);
                string day = datetimeX.Substring(8, 2);
                string hour = datetimeX.Substring(11, 2);
                string minute = datetimeX.Substring(14, 2);
                string second = datetimeX.Substring(17, 2);


                Debug.Print("Year: " + year);
                Debug.Print("Month: " + month);
                Debug.Print("Day: " + day);
                Debug.Print("Hour: " + hour);
                Debug.Print("Minute: " + minute);
                Debug.Print("Second: " + second);

                DateTime time = new DateTime(Int32.Parse(year), Int32.Parse(month), Int32.Parse(day), Int32.Parse(hour), Int32.Parse(minute), Int32.Parse(second));
                return time;
            }
            catch (System.Net.WebException wex)
            {
                Debug.Print("WebAPI Exception: " + wex.Message);
                DateTime time = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
                return time;
            }
            catch (Exception ex)
            {
                Debug.Print("WebAPI Exception: " + ex.Message);
                DateTime time = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
                return time;
            }
        }
        #endregion

        #region SD Card Methods
        //Read config from SD card
        private void loadConfigFile()
        {
            string filePath = "Application.config";
            try
            {
                if (!sdCard.IsCardMounted)
                {
                    sdCard.Mount();
                }

                byte[] myFile = sdCard.StorageDevice.ReadFile(filePath);
                using (Stream configStream = sdCard.StorageDevice.OpenRead(filePath))
                {
                    ConfigurationManager.Load(configStream);
                }

                //Retrieve values
                config = new Configuration();
                config.deviceId = ConfigurationManager.GetAppSettings("deviceId");
                config.sasUrl = ConfigurationManager.GetAppSettings("sasUrl");
                config.sasKey = ConfigurationManager.GetAppSettings("sasKey");
                config.ssid = ConfigurationManager.GetAppSettings("ssid");
                config.password = ConfigurationManager.GetAppSettings("password");

            }
            catch (Exception ex)
            {
                Debug.Print("Error: " + ex.Message);
                throw ex;
            }
        }

        //Verify that an SD card is loaded
        private bool verifySDCard()
        {
            if (!sdCard.IsCardInserted || !sdCard.IsCardMounted)
            {
                Debug.Print("Insert SD card!");
                return false;
            }
            return true;
        }

        private void saveImageSDCard(GT.Picture picture, string fileName)
        {
            if (verifySDCard())
            {
                string pathFileName = "\\SD\\" + fileName;
                Debug.Print("pathFileName: " + pathFileName);

                try
                {
                    sdCard.StorageDevice.WriteFile(pathFileName, picture.PictureData);
                    Debug.Print("Image saved to SD Card: " + pathFileName);
                }
                catch (Exception ex)
                {
                    Debug.Print("Error: " + ex.Message);
                }
            }
        }
        #endregion

        #region Azure Methods
        private void insertImageintoAzureBlob(GT.Picture picture)
        {
            AzureBlob storage = new AzureBlob();

            bool error = false;

            if (wifi.IsNetworkUp)
            {
                try
                {
                    storage.PutBlob(config, picture.PictureData, error);
                    if (error)
                    {
                        Debug.Print("Error: " + error.ToString());
                    }
                    else
                    {
                        Debug.Print("There was no error via PutBlob.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print("EXCEPTION: " + ex.Message);
                }
            }
            else
            {
                Debug.Print("NO NETWORK CONNECTION");
            }
        }
        #endregion

        #region Character Display methods
        private void displayMessage(string message)
        {
            characterDisplay.Clear();
            characterDisplay.CursorHome();
            characterDisplay.Print(message);
        }

        private void displayMessage(string message1, string message2)
        {
            characterDisplay.Clear();
            characterDisplay.CursorHome();
            characterDisplay.Print(message1 + "\n");
            characterDisplay.Print(message2);
        }
        #endregion

        #region Hardware Interupts
        //Interupts
        void scrollDown_ButtonPressed(Button sender, Button.ButtonState state)
        {
            throw new NotImplementedException();
        }

        void scrollUp_ButtonPressed(Button sender, Button.ButtonState state)
        {
            throw new NotImplementedException();
        }

        void motionSensor_Motion_Sensed(MotionSensor sender, MotionSensor.Motion_SensorState state)
        {
            try
            {
                if (camera.CameraReady)
                {
                    camera.TakePicture();
                }
                else
                {
                    Debug.Print("Camera not ready, no picutre taken");
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                throw ex;
            }
        }

        void camera_PictureCaptured(Camera sender, GT.Picture picture)
        {
            try
            {
                Debug.Print("picture.PictureData.Length: " + picture.PictureData.Length.ToString());
                if (picture.PictureData.Length > 0)
                {
                    string newFileName = currentFile.ToString() + ".jpg";
                    insertImageintoAzureBlob(picture);
                    saveImageSDCard(picture, newFileName);
                }
                else
                {
                    Debug.Print("Image not found, has a length <= 0");
                }

            }
            catch (Exception ex)
            {
                Debug.Print("Error: " + ex.Message);
                //multicolorLED.BlinkRepeatedly(GT.Color.Red);
            }
        }
        #endregion


    }
}
