namespace kauSupport.Models;

public class DeviceReports
{
    public Device device { get; set; }
    public IEnumerable<Report> reports { get; set; }

    
}

