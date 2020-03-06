using System.ComponentModel.DataAnnotations;

namespace WebApp.Core
{
    public class PrinterConnectionSettings
    {
        [Required]
        public string PrinterName { get; set; }

        [Required]
        public string UsbPort { get; set; }

        [Required]
        public int Baudrate { get; set; }
    }
}
