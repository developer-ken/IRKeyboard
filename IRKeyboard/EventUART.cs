using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace IRKeyboard
{
    class EventUART
    {
        public UART uart;
        private Thread receiver;
        public bool ThreadRunFlag = true;
        public event Action<string> LineReceived;

        public EventUART(string portname,int badurate)
        {
            uart = new UART(portname);
            uart.port.BaudRate = badurate;
            uart.Init();
            receiver = new Thread(new ThreadStart(() =>
             {
                 while (ThreadRunFlag)
                 {
                     string line = uart.ReadLine();
                     LineReceived?.Invoke(line);
                 }
             }));
            receiver.Start();
        }
    }
}
