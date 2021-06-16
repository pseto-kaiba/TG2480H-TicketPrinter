using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using CustomPrinter;
using Newtonsoft.Json;

namespace PrinterTest
{
    class Program
    {
        static void Main(string[] args)
        {
            
            //Console.WriteLine(CustomPrinter.PrinterLogic.asteriskCardNum("9999999999999438131"));
            string ticket_s = "{'TerminalID': 'UPTTEST1','CardIssuer': 'VISA','AID': 'A0000000032010','CardApplicationName': 'Visa Electron','CardNumber': '4381319999999999999','TransactionType': '01','InvoiceNumber': '000033','TransactionDate': '010621','TransactionTime': '133116','RRN': '444406084444','ApprovalCode': '253680','ResponseCode': '00','TransactionStatus': 'ODOBRENO','Amount': '0.10','EMVData': 'ARQC (80) 3AECCB93EE745EDC','InstallmentsNumber': '','SignatureLine': '1'}";
            //string header_s = "{'name' : 'TEST PRODAJNI APARAT','URL' : 'www.simkela.com','phoneNumber' : '011 578 45 89','address' : 'Marije Terezije 18','location' : 'BEOGRAD, SRBIJA','dateTime': '22:07:48 31.5.2021.'}";
            CustomPrinter.Ticket t = JsonConvert.DeserializeObject<Ticket>(ticket_s);
            //var img = Image.FromFile("raif_mono.png");
            CustomPrinter.PrinterController pc = new CustomPrinter.PrinterController("printerConfig.xml");
            var tst = pc.Print(t);
            //CustomPrinter.PrinterLogic.print(t, img);
            //CustomPrinter.PrinterLogic.testPrinter(img);
            Console.ReadKey();
        }
    }
}
