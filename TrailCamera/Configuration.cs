using System;
using Microsoft.SPOT;

namespace TrailCamera
{
    public class Configuration
    {
        public string deviceId { get; set; }
        public string sasUrl { get; set; }
        public string sasKey { get; set; }
        public string ssid { get; set; }
        public string password { get; set; }
    }
}
