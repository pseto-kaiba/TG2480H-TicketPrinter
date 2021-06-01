# TG2480H-TicketPrinter
.NET 4.6.1 controller software for the TG2480H thermal POS printer. Requires a COM port (real or virtual). 

Usage:


//Instantiate the printer controller object. The xml config file is mandatory and provides info about the COM port, baudrate, any receipt logo (optional: leave the FileName 
//element blank if you don't want one) and the entire receipt header, with an option to make each line of the header enlargened. Consult the example config file.
PrinterController pc = new PrinterController("printerConfig.xml");


//Create an object of type Ticket one way or another - this is a simple objectified JSON file
Ticket t = ...;


//Print the ticket. ps_ret is a class wrapper returning information about the print job.
//This includes the number of errors preventing the print job from executing, the full 6 byte status code of the device (consult the DLE EOT section of the command manual), a //description of each relevant printer state and an additional indicator whether connecting via the COM port was successful


PrinterStatus ps_ret = pc.Print(t);

