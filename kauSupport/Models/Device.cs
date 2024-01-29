namespace kauSupport.Models;

public class Device
{
 
        public string serialNumber { get; set; }
        public int deviceNumber { get; set; }
        public string deviceStatus { get; set; }
        public string type { get; set; }
        public string deviceLocatedLab { get; set; }
        public DateTime  arrivalDate { get; set; }
        public DateTime  nextPeriodicDate { get; set; }

    } 
