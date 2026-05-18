using System.Collections.Generic;

namespace linx.core.reporthandler
{
    public class Telegram
    {
        public Telegram(LinxPrintJobTypeEnum jobType, byte[] packet)
        {
            LinxPrintJobType = jobType;
            Packet = packet;
        }

        public LinxPrintJobTypeEnum LinxPrintJobType { get; set; }
        public byte[] Packet { get; set; }
    }
}
