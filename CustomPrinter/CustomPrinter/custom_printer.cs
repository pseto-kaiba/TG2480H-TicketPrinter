using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace CustomPrinter
{

    [Serializable]
    public class Ticket
    {
        public string TerminalID { get; set; }
        public string CardIssuer { get; set; }
        public string AID { get; set; }
        public string CardApplicationName { get; set; }
        public string CardNumber { get; set; }
        public string TransactionType { get; set; }
        public string InvoiceNumber { get; set; }
        public string TransactionDate { get; set; }
        public string TransactionTime { get; set; }
        public string RRN { get; set; }
        public string ApprovalCode { get; set; }
        public string ResponseCode { get; set; }
        public string TransactionStatus { get; set; }
        public string Amount { get; set; }
        public string EMVData { get; set; }
        public string InstallmentsNumber { get; set; }
        public string SignatureLine { get; set; }
    }


    public class PrinterStatus
    {
        public int numPrinterErrors { get; set; }
        public List<string> printerStatusDesc { get; set; }
        public bool COMPortAvailable { get; set; }
        public byte[] fullStatus {get; set;}
    }


    public class PrinterController
    {



        //Width of receipt in WorldUnits
        private int ticketWidth = 250;
        //Font of receipt title
        private Font tit_fnt = new Font("Times New Roman", 18);
        //Font of bolded receipt body
        private Font b_fnt = new Font("Sans Serif", 10, FontStyle.Bold);
        //Font of receipt body
        private Font fnt = new Font("Sans Serif", 7);

        //Font of receipt header
        private Font h_fnt = new Font("Sans Serif", 8);

        private XDocument config_file = null;
        private string COMPort = "";
        private SerialPort port;

        private Image img = null;
        private int img_height = 0;


        public PrinterController(string configPath, int _ticketWidth = 250)
        {
            ticketWidth = _ticketWidth;
            config_file = XDocument.Load(configPath);
            var com_config = config_file.Element("config").Element("COMconfig");
            COMPort = com_config.Element("COMPort").Value.ToString();
            var baud = Int32.Parse(com_config.Element("BaudRate").Value.ToString());
            var logo_config = config_file.Element("config").Element("logo");
            try
            {
                var fname = logo_config.Element("FileName").Value.ToString();
                img = Image.FromFile(fname);
                img_height = Int32.Parse(logo_config.Element("Height").Value.ToString());
            }
            catch (Exception) { }
            port = new SerialPort(COMPort, baud);
            //port.Open();
        }

        ~PrinterController()
        {
            port?.Dispose();
        }


        public PrinterController(string configPath, Font _tit_fnt, Font _b_fnt, Font _fnt, Font _h_fnt, int _ticketWidth = 250): this(configPath, _ticketWidth)
        {

            //ticketWidth = _ticketWidth;
            tit_fnt = _tit_fnt;
            b_fnt = _b_fnt;
            fnt = _fnt;
            h_fnt = _h_fnt;
            /*config_file = XDocument.Load(configPath);
            var com_config = config_file.Element("config").Element("COMconfig");
            COMPort = com_config.Element("COMPort").Value.ToString();
            var baud = Int32.Parse(com_config.Element("BaudRate").Value.ToString());
            ticketWidth = _ticketWidth;
            var logo_config = config_file.Element("logo");
            try
            {
                img = Image.FromFile(logo_config.Element("FileName").Value.ToString());
                img_height = Int32.Parse(logo_config.Element("Height").Value.ToString());
            }
            catch (Exception) { }
            port = new SerialPort(COMPort, baud);
            writer = new BinaryWriter(port.BaseStream);
            reader = new BinaryReader(port.BaseStream);*/
        }


        public PrinterStatus checkStatus()
        {
            PrinterStatus ret = new PrinterStatus();
            ret.numPrinterErrors = 0;
            ret.printerStatusDesc = new List<string>();
            ret.COMPortAvailable = true;
            try
            {
                port.Open();
                var writer = new BinaryWriter(port.BaseStream);
                var reader = new BinaryReader(port.BaseStream);
                port.Write("" + (char)0x10 + (char)0x4 + (char)0x14);
                var fullStatus = reader.ReadBytes(6);
                byte paperStatus = fullStatus[2];
                byte userStatus = fullStatus[3];
                byte recoverableStatus = fullStatus[4];
                byte unrecoverableStatus = fullStatus[5];
                port.Close();
                ret.fullStatus = new byte[6];
                Array.Copy(fullStatus, ret.fullStatus, 6);
                if ((paperStatus & 1 << 0) != 0) { ret.numPrinterErrors++; ret.printerStatusDesc.Add("NO PAPER ERROR"); }
                if ((paperStatus & 1 << 2) != 0) { ret.printerStatusDesc.Add("LOW PAPER"); }
                if ((paperStatus & 1 << 5) != 0) { ret.printerStatusDesc.Add("TICKET PRESENT IN OUTPUT"); }
                if ((paperStatus & 1 << 6) != 0) { ret.printerStatusDesc.Add("VIRTUAL PAPER END"); }
                if ((paperStatus & 1 << 7) != 0) { ret.printerStatusDesc.Add("BLACK MARK NOT PLACED OVER THE SENSOR"); }

                if ((userStatus & 1 << 0) != 0) { ret.numPrinterErrors++; ret.printerStatusDesc.Add("PRINTING HEAD UP ERROR"); }
                if ((userStatus & 1 << 1) != 0) { ret.numPrinterErrors++; ret.printerStatusDesc.Add("COVER OPEN ERROR"); }
                if ((userStatus & 1 << 5) != 0) { ret.numPrinterErrors++; ret.printerStatusDesc.Add("LF KEY PRESSED"); }
                if ((userStatus & 1 << 6) != 0) { ret.numPrinterErrors++; ret.printerStatusDesc.Add("FF KEY PRESSED"); }

                if ((recoverableStatus & 1 << 0) != 0) { ret.numPrinterErrors++; ret.printerStatusDesc.Add("HEAD TEMPERATURE ERROR"); }
                if ((recoverableStatus & 1 << 1) != 0) { ret.numPrinterErrors++; ret.printerStatusDesc.Add("RS232 COM ERROR"); }
                if ((recoverableStatus & 1 << 3) != 0) { ret.numPrinterErrors++; ret.printerStatusDesc.Add("POWER SUPPLY VOLTAGE ERROR"); }
                if ((recoverableStatus & 1 << 5) != 0) { ret.numPrinterErrors++; ret.printerStatusDesc.Add("NOT ACKNOWLEDGE COMMAND ERROR"); }
                if ((recoverableStatus & 1 << 6) != 0) { ret.numPrinterErrors++; ret.printerStatusDesc.Add("PAPER JAM ERROR"); }
                if ((recoverableStatus & 1 << 7) != 0) { ret.numPrinterErrors++; ret.printerStatusDesc.Add("BLACK MARK SEARCH ERROR"); }

                if ((unrecoverableStatus & 1 << 0) != 0) { ret.numPrinterErrors++; ret.printerStatusDesc.Add("AUTOCUTTER ERROR"); }
                if ((unrecoverableStatus & 1 << 1) != 0) { ret.numPrinterErrors++; ret.printerStatusDesc.Add("AUTOCUTTER COVER OPEN ERROR"); }
                if ((unrecoverableStatus & 1 << 2) != 0) { ret.numPrinterErrors++; ret.printerStatusDesc.Add("RAM ERROR"); }
                if ((unrecoverableStatus & 1 << 3) != 0) { ret.numPrinterErrors++; ret.printerStatusDesc.Add("EEPROM ERROR"); }
            }
            catch (Exception)
            {
                ret.COMPortAvailable = false;
                ret.numPrinterErrors++;
                ret.printerStatusDesc.Add("COM PORT UNAVAILABLE ERROR");
            }


            return ret;
        }

        private static string padder(int n, char c = ' ')
        {
            var s = new StringBuilder();
            for (int i = 0; i < n; i++) s.Append(c);
            return s.ToString();
        }


        private void DrawCombinedLine(Font fnt, Graphics g, float x, ref float y, string sLeft, string sCenter = "", string sRight = "")
        {
            var sf1 = new StringFormat();
            var sf2 = new StringFormat();
            var sf3 = new StringFormat();
            sf1.Alignment = StringAlignment.Near;
            sf2.Alignment = StringAlignment.Center;
            sf3.Alignment = StringAlignment.Far;

            sLeft += "  ";
            sCenter += "  ";



            int dy = (int)fnt.GetHeight(g) * 1;
            RectangleF print_rect = new RectangleF(x, y, ticketWidth - x, dy);
            g.DrawString(sLeft, fnt, Brushes.Black, print_rect, sf1);
            g.DrawString(sCenter, fnt, Brushes.Black, print_rect, sf2);
            g.DrawString(sRight, fnt, Brushes.Black, print_rect, sf3);
            y += dy;
        }


        private void DrawLogo(Image img, Graphics g, float x, ref float y, float height = -1)
        {
            if (height <= 0) height = img.Height;
            RectangleF dest_rect = new RectangleF(x, y, ticketWidth - x, height);
            RectangleF source_rect = new RectangleF(0, 0, img.Width, img.Height);
            g.DrawImage(img, dest_rect, source_rect, GraphicsUnit.Pixel);
            //g.DrawImage(img, new Point(0, 0));
            y += height;
        }

        private static string asteriskCardNum(string cardNumber, char replaceChar = '*')
        {
            StringBuilder sb = new StringBuilder(cardNumber);
            int nineIndex = cardNumber.IndexOf("99999999", 0, cardNumber.Length);
            if (nineIndex == 0) for (int i = 0; i < cardNumber.Length && cardNumber[i] == '9'; i++) sb[i] = replaceChar;
            else for (int i = cardNumber.Length - 1; i >= 0 && cardNumber[i] == '9'; i--) sb[i] = replaceChar;
            return sb.ToString();
        }

        private void generatePrintContent(object sender, PrintPageEventArgs e, Ticket t)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            float x = 0; float y = 0;
            var ticket_dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(t));

            if (img != null) DrawLogo(img, g, x, ref y, 100);

            if (config_file != null)
            {
                var header = config_file.Element("config").Element("ticketHeader");
                foreach (var element in header.Elements())
                {
                    if (element == null) continue;
                    Font pr_fnt = h_fnt;
                    if (element.Attribute("bigger") != null && element.Attribute("bigger").Value.ToString().ToUpper() == "YES") pr_fnt = tit_fnt;
                    DrawCombinedLine(pr_fnt, g, x, ref y, "", element.Value.ToString(), "");
                }
            }

            DrawCombinedLine(fnt, g, x, ref y, "", "******************************************************************************************");
            DrawCombinedLine(fnt, g, x, ref y, "TERMINAL ID", "", t.TerminalID);
            DrawCombinedLine(b_fnt, g, x, ref y, t.CardIssuer, "", "");
            DrawCombinedLine(fnt, g, x, ref y, "AID " + t.AID, "", "APN " + t.CardApplicationName);
            DrawCombinedLine(fnt, g, x, ref y, asteriskCardNum(t.CardNumber), "", "VAŽI DO (MMGG) **/**");
            DrawCombinedLine(b_fnt, g, x, ref y, "PRODAJA", "", "");
            DrawCombinedLine(fnt, g, x, ref y, "TIP TRANSAKCIJE " + t.TransactionType, "", "BR. POTVRDE " + t.InvoiceNumber);
            string date = DateTime.ParseExact(t.TransactionDate, "ddMMyy", CultureInfo.InvariantCulture).ToString("dd.MM.yyyy");
            string time = t.TransactionTime;
            time = "" + time[0] + time[1] + ":" + time[2] + time[3] + ":" + time[4] + time[5];
            DrawCombinedLine(fnt, g, x, ref y, "DATUM " + date, "", "VREME " + time);
            DrawCombinedLine(fnt, g, x, ref y, "RRN " + t.RRN, "", "(D1)BR. ODOBRENJA " + t.ApprovalCode);
            DrawCombinedLine(fnt, g, x, ref y, "RESP " + t.ResponseCode, "", t.TransactionStatus);
            DrawCombinedLine(fnt, g, x, ref y, (t.InstallmentsNumber != "" ? "BROJ RATA " + t.InstallmentsNumber : ""), "", "POTPIS POTREBAN " + (t.SignatureLine == "0" ? "DA" : "NE"));
            DrawCombinedLine(b_fnt, g, x, ref y, "IZNOS", "", t.Amount + " RSD");
            DrawCombinedLine(fnt, g, x, ref y, "", "\n", "");
            DrawCombinedLine(fnt, g, x, ref y, "", "HVALA", "");
            DrawCombinedLine(fnt, g, x, ref y, "", "MOLIMO SAČUVAJTE RAČUN", "");
            DrawCombinedLine(fnt, g, x, ref y, "", "** KOPIJA ZA KORISNIKA **", "");
            DrawCombinedLine(fnt, g, x, ref y, "", "\n", "");
            DrawCombinedLine(fnt, g, x, ref y, "", "******************************************************************************************");
            DrawCombinedLine(fnt, g, x, ref y, t.EMVData, "", "");
            /*foreach (var pair in ticket_dict)
            {
                string skey = pair.Key;
                string sval = pair.Value;
                string sLeft = "", sCenter = "", sRight = "";
                Font pr_fnt;
                if (skey == "CardNumber") sval = asteriskCardNum(sval);
                

                if (skey == "CardIssuer")
                {
                    pr_fnt = b_fnt;
                    sLeft = sval.ToUpper();
                }
                else
                {
                    pr_fnt = fnt;
                    sLeft = skey.ToUpper();
                    sRight = sval.ToUpper();
                }
                if (sLeft != "" || sCenter != "" || sRight != "")
                {
                    DrawCombinedLine(pr_fnt, g, x, ref y, sLeft, sCenter, sRight);
                    //DrawCombinedLine(pr_fnt, g, x, ref y, "");
                }
            }*/

        }

        public PrinterStatus Print(Ticket t)
        {
            var status = checkStatus();
            if (status.numPrinterErrors == 0)
            {
                var doc = new PrintDocument();
                doc.PrintPage += (sender, e) => generatePrintContent(sender, e, t);
                doc.Print();
            }
            return status;
        }

        private void examplePrint(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            /*g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;*/
            float x = 0; float y = 0;
            if (img != null) DrawLogo(img, g, x, ref y, 100);
            DrawCombinedLine(tit_fnt, g, x, ref y, "", "www.raiffeisenbank.rs", "");
            DrawCombinedLine(fnt, g, x, ref y, "", "KLANICA MILAN STEVAN ČAPELJA PR DOBANOVC", "");
            DrawCombinedLine(fnt, g, x, ref y, "", "Klanica Milan 7", "");
            DrawCombinedLine(fnt, g, x, ref y, "", "SLANAČKI PUT 95B", "");
            DrawCombinedLine(fnt, g, x, ref y, "", "BEOGRAD", "");
            DrawCombinedLine(fnt, g, x, ref y, "", "\n", "");
            DrawCombinedLine(fnt, g, x, ref y, "TERMINAL", "", "RB100888");
            DrawCombinedLine(fnt, g, x, ref y, "TERMINAL ID", "", "2299001B");
            DrawCombinedLine(fnt, g, x, ref y, "AKCEPTANT", "", "625481692299FFB");
            DrawCombinedLine(b_fnt, g, x, ref y, "VISA", "", "");
            DrawCombinedLine(fnt, g, x, ref y, "AID A0000000032010", "", "APN Visa Electron");
            DrawCombinedLine(fnt, g, x, ref y, "TVR 0000008000", "TSI F800", "CVMR 440302");
            DrawCombinedLine(fnt, g, x, ref y, "************7166", "", "VAŽI DO (MMGG) **/**");
            DrawCombinedLine(b_fnt, g, x, ref y, "PRODAJA", "", "");
            DrawCombinedLine(fnt, g, x, ref y, "BR. PROMETA 002731", "", "Br. POTVRDE 046365");
            DrawCombinedLine(fnt, g, x, ref y, "DATUM 24.05.2021", "", "VREME 16:54:06");
            DrawCombinedLine(fnt, g, x, ref y, "RRN 114432498548", "", "(D1)BR. ODOBRENJA 225193");
            DrawCombinedLine(fnt, g, x, ref y, "RESP 00", "", "ODOBRENO");
            DrawCombinedLine(fnt, g, x, ref y, "", "\n", "");
            DrawCombinedLine(b_fnt, g, x, ref y, "IZNOS", "", "521.56 RSD");
            DrawCombinedLine(fnt, g, x, ref y, "JOVANOVIC/BRATISLAV", "", "");
            DrawCombinedLine(fnt, g, x, ref y, "", "\n", "");
            DrawCombinedLine(fnt, g, x, ref y, "", "HVALA", "");
            DrawCombinedLine(fnt, g, x, ref y, "", "MOLIMO SAČUVAJTE RAČUN", "");
            DrawCombinedLine(fnt, g, x, ref y, "", "** KOPIJA ZA KORISNIKA **", "");
            DrawCombinedLine(fnt, g, x, ref y, "", "\n", "");
            DrawCombinedLine(fnt, g, x, ref y, "TC    (40) D0FEE69F0AAF1D18", "", "");
            DrawCombinedLine(fnt, g, x, ref y, "B: 200611/01", "", "");
            DrawCombinedLine(fnt, g, x, ref y, "", "\n", "");
        }

        public void testPrinter(Image logo=null)
        {
            var doc = new PrintDocument();
            doc.PrintPage += (sender, e) => examplePrint(sender, e);
            doc.Print();
        }

    }
}
