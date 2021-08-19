using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;


namespace SerialAPDUTest
{
    public partial class Form1 : Form
    {
        static SerialPort _serialPort;
        static bool _continue;
        delegate void TextBox_Invoke(string data);
        delegate void DebugText_Invoke(string message, bool isWriteToLabel);

        public Form1()
        {
            InitializeComponent();

            _serialPort = new SerialPort();

            #region 포트번호 등 설정값 입력

            foreach (string s in SerialPort.GetPortNames())
            {
                comboBoxPort.Items.Add(s);
            }

            if (comboBoxPort.Items.Count > 0)
                comboBoxPort.SelectedIndex = 0;

            // baudRate 설정 : 4800, 9600, 19200, 38400(default baud rate) and 115200 bps
            comboBoxBaudRate.Items.Add("4800");
            comboBoxBaudRate.Items.Add("9600");
            comboBoxBaudRate.Items.Add("19200");
            comboBoxBaudRate.Items.Add("38400");
            comboBoxBaudRate.Items.Add("115200");

            comboBoxBaudRate.SelectedIndex = comboBoxBaudRate.FindString(_serialPort.BaudRate.ToString());

            // parity set
            foreach (string s in Enum.GetNames(typeof(Parity)))
            {
                comboBoxParity.Items.Add(s);
            }

            comboBoxParity.SelectedIndex = comboBoxParity.FindString(_serialPort.Parity.ToString());

            // data bits
            textBoxDataBits.Text = _serialPort.DataBits.ToString();

            // stop bits
            foreach (string s in Enum.GetNames(typeof(StopBits)))
            {
                comboBoxStopBits.Items.Add(s);
            }
            comboBoxStopBits.SelectedIndex = comboBoxStopBits.FindString(_serialPort.StopBits.ToString());

            // handshake options
            foreach (string s in Enum.GetNames(typeof(Handshake)))
            {
                comboBoxHandShake.Items.Add(s);
            }
            comboBoxHandShake.SelectedIndex = comboBoxHandShake.FindString(_serialPort.Handshake.ToString());

            textBoxReadTimeout.Text = "500";
            textBoxWriteTimeout.Text = "500";
            #endregion
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            // Allow the user to set the appropriate properties.
            if(comboBoxPort.SelectedItem == null)
            {
                DebugMessage("Port가 선택되지 않았습니다.");
                MessageBox.Show("Port가 선택되지 않았습니다.");
                return;
            }

            _serialPort.PortName = comboBoxPort.SelectedItem.ToString();
            _serialPort.BaudRate = int.Parse(comboBoxBaudRate.SelectedItem.ToString());
            _serialPort.Parity = (Parity)Enum.Parse(typeof(Parity), comboBoxParity.SelectedItem.ToString(), true);
            _serialPort.DataBits = int.Parse(textBoxDataBits.Text);
            _serialPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), comboBoxStopBits.SelectedItem.ToString(), true);
            _serialPort.Handshake = (Handshake)Enum.Parse(typeof(Handshake), comboBoxHandShake.SelectedItem.ToString(), true);

            // Set the read/write timeouts
            _serialPort.ReadTimeout = int.Parse(textBoxReadTimeout.Text);
            _serialPort.WriteTimeout = int.Parse(textBoxWriteTimeout.Text);

            _serialPort.DataReceived += new SerialDataReceivedEventHandler(Serial_DataReceived);

            try
            {
                _serialPort.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                DebugMessage(ex.Message);
                MessageBox.Show(ex.Message);
                return;
            }

            DebugMessage("Serial 연결에 성공했습니다.");

            _continue = true;
            buttonConnect.Enabled = false;
            buttonClose.Enabled = true;
        }

        private void Serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort port = (SerialPort)sender;
            byte[] data = new byte[port.BytesToRead];
            port.Read(data, 0, data.Length);
            string message = ByteToHexaString(data);
            Console.WriteLine(message);
            WriteDataToOutput(message);
        }

        public void Read()
        {
            while (_continue)
            {
                try
                {
                    string message = _serialPort.ReadLine();
                    Console.WriteLine(message);
                    textBoxOutput.Text += string.Format("{0}\r\n", message);
                    textBoxOutput.Text += string.Format("{0}\r\n", ByteToHexaString(Encoding.Default.GetBytes(message)));
                }
                catch (TimeoutException) { }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        // Display Port values and prompt user to enter a port.
        public static string SetPortName(string defaultPortName)
        {
            string portName;

            Console.WriteLine("Available Ports:");
            foreach (string s in SerialPort.GetPortNames())
            {
                Console.WriteLine("   {0}", s);
            }

            Console.Write("Enter COM port value (Default: {0}): ", defaultPortName);
            portName = Console.ReadLine();

            if (portName == "" || !(portName.ToLower()).StartsWith("com"))
            {
                portName = defaultPortName;
            }
            return portName;
        }
        // Display BaudRate values and prompt user to enter a value.
        public static int SetPortBaudRate(int defaultPortBaudRate)
        {
            string baudRate;

            Console.Write("Baud Rate(default:{0}): ", defaultPortBaudRate);
            baudRate = Console.ReadLine();

            if (baudRate == "")
            {
                baudRate = defaultPortBaudRate.ToString();
            }

            return int.Parse(baudRate);
        }

        // Display PortParity values and prompt user to enter a value.
        public static Parity SetPortParity(Parity defaultPortParity)
        {
            string parity;

            Console.WriteLine("Available Parity options:");
            foreach (string s in Enum.GetNames(typeof(Parity)))
            {
                Console.WriteLine("   {0}", s);
            }

            Console.Write("Enter Parity value (Default: {0}):", defaultPortParity.ToString(), true);
            parity = Console.ReadLine();

            if (parity == "")
            {
                parity = defaultPortParity.ToString();
            }

            return (Parity)Enum.Parse(typeof(Parity), parity, true);
        }
        // Display DataBits values and prompt user to enter a value.
        public static int SetPortDataBits(int defaultPortDataBits)
        {
            string dataBits;

            Console.Write("Enter DataBits value (Default: {0}): ", defaultPortDataBits);
            dataBits = Console.ReadLine();

            if (dataBits == "")
            {
                dataBits = defaultPortDataBits.ToString();
            }

            return int.Parse(dataBits.ToUpperInvariant());
        }

        // Display StopBits values and prompt user to enter a value.
        public static StopBits SetPortStopBits(StopBits defaultPortStopBits)
        {
            string stopBits;

            Console.WriteLine("Available StopBits options:");
            foreach (string s in Enum.GetNames(typeof(StopBits)))
            {
                Console.WriteLine("   {0}", s);
            }

            Console.Write("Enter StopBits value (None is not supported and \n" +
             "raises an ArgumentOutOfRangeException. \n (Default: {0}):", defaultPortStopBits.ToString());
            stopBits = Console.ReadLine();

            if (stopBits == "")
            {
                stopBits = defaultPortStopBits.ToString();
            }

            return (StopBits)Enum.Parse(typeof(StopBits), stopBits, true);
        }
        public static Handshake SetPortHandshake(Handshake defaultPortHandshake)
        {
            string handshake;

            Console.WriteLine("Available Handshake options:");
            foreach (string s in Enum.GetNames(typeof(Handshake)))
            {
                Console.WriteLine("   {0}", s);
            }

            Console.Write("Enter Handshake value (Default: {0}):", defaultPortHandshake.ToString());
            handshake = Console.ReadLine();

            if (handshake == "")
            {
                handshake = defaultPortHandshake.ToString();
            }

            return (Handshake)Enum.Parse(typeof(Handshake), handshake, true);
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            _continue = false;
            _serialPort.Close();
            buttonConnect.Enabled = true;
            buttonClose.Enabled = false;
        }

        private void buttonSend_Click(object sender, EventArgs e)
        {
            if(_continue)
            {
                string sendText = textBoxInput.Text.Replace(" ", "");

                textBoxOutput.AppendText(string.Format("[{0}] Send : \r\n{1}\r\n", System.DateTime.Now.ToString("HH:mm:ss"), MakeHexaSpace(sendText)));
                byte[] sendData = HexaStringtoByte(sendText);
                try
                {
                    _serialPort.Write(sendData, 0, sendText.Length / 2);
                }
                catch(Exception ex)
                {
                    DebugMessage(ex.Message);
                    Console.WriteLine(ex);
                    MessageBox.Show(ex.Message);
                    return;
                }

                textBoxOutput.Clear();
            }
        }

        private string MakeHexaSpace(string text)
        {
            string newText = string.Empty;

            int counter = 1;
            foreach(char character in text)
            {
                newText += character;
                if (counter % 2 == 0) newText += " ";
                if (counter % 20 == 0)
                    newText += "\r\n";
                //    newText += "\t" + Encoding.Unicode.GetString(HexaStringtoByte(text.Substring(counter - 20, 20))) + "\r\n";
                counter++;
            }
            return newText.ToUpper();
        }

        public string StringtoHexaString(string data)
        {
            string resultString = string.Empty;

            byte[] stringArray = Encoding.Default.GetBytes(data);

            foreach (byte character in stringArray)
                resultString += string.Format("{0:X2}", character);

            return resultString;
        }

        public byte[] HexaStringtoByte(string data)
        {
            byte[] resultArray = new byte[data.Length / 2];

            for (int i = 0; i < resultArray.Length; i++)
                resultArray[i] = Convert.ToByte(data.Substring(i * 2, 2), 16);

            return resultArray;
        }

        public string ByteToHexaString(byte[] data)
        {
            string resultString = string.Empty;

            resultString = string.Concat(Array.ConvertAll(data, byt => byt.ToString("X2")));

            return resultString;
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            textBoxOutput.Clear();
        }

        private void WriteDataToOutput(string message)
        {
            if (textBoxOutput.InvokeRequired)
            {
                TextBox_Invoke invoke = new TextBox_Invoke(WriteDataToOutput);
                textBoxOutput.Invoke(invoke, message);
            }
            else
            {
                textBoxOutput.AppendText(string.Format("[{0}] Recv : \r\n{1}\r\n", System.DateTime.Now.ToString("HH:mm:ss"), MakeHexaSpace(message)));
            }
        }

        private void DebugMessage(string message, bool isWriteToLabel = true)
        {
            if (labelDebug.InvokeRequired)
            {
                DebugText_Invoke invoke = new DebugText_Invoke(DebugMessage);
                labelDebug.Invoke(invoke, message, isWriteToLabel);
            }
            else
            {
                string logMessage = string.Format("[{0}] {1}", System.DateTime.Now.ToString("HH:mm:ss"), message);
                Console.WriteLine(logMessage);
                if(isWriteToLabel == true)
                    labelDebug.Text = logMessage;
            }
        }
    }
}
