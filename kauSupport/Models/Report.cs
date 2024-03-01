namespace kauSupport.Models;

public class Report
{
    public int reportID { get; set; }
    public string deviceNumber { get; set; }
    public string serialNumber { get; set; }
    public string deviceLocatedLab { get; set; }
    public string reportType { get; set; }
    public string reportStatus { get; set; }
    public string problemDescription { get; set; }
    public string reportedBy { get; set; }
    public DateTime reportDate { get; set; }
    public DateTime repairDate { get; set; }
    public string actionTaken { get; set; }
    public string assignedTaskTo { get; set; }
    public string problemType { get; set; }
    public string  reportedByFirstName { get; set; }
    public string reportedByLastName { get; set; }
    public string   assignedToFirstName { get; set; }
    public string  assignedToLastName { get; set; }

}