namespace kauSupport.Models;

public class Service

{
  
        public int RequestID { get; set; }
        public string RequestedBy { get; set; }
        public string RequestStatus { get; set; }
        public string TechnicalSupportReply { get; set; }
        public string Request { get; set; } 
        public string AssignedTo { get; set; } 
        public string  requestedByFirstName { get; set; }
        public string requestedByLastName { get; set; }
        public string   assignedToFirstName { get; set; }
        public string  assignedToLastName { get; set; }
        public string  requestType { get; set; }

    
}

