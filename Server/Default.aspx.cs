using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;
using Ionic.Zip;


namespace ServerClientCommunication
{
    public partial class _Default : System.Web.UI.Page
    {
        public static string FeatureExtractionCommand;
        public static string PocketSphinxMdefConversionCommand;
        public static string CopyToNewModel;
        //public static string SendumpCommand;
        public static string BWCommand;
        public static string MapAdaptationCommand;
        public static string MkS2SendumpCommand;
        public static string TestAdaptationCommand;
        public static string DecodeTestCommand;
        public static string DeleteFileCommand;

        protected void Page_Load(object sender, EventArgs e)
        {

        }

        public class SynchronousSocketListener
        {

            // Incoming data from the client.
            public string data = null;
            public string message = null;
            public string server = @"C:\Users\DANIEL\Documents\visual studio 2010\Projects\ServerClientCommunication\ServerClientCommunication\";

            public SynchronousSocketListener()
            {
                GetCommands();
            }

            public void StartListening()
            {
                // Data buffer for incoming data.
                byte[] bytes = new Byte[1024];

                // Establish the local endpoint for the socket.
                // Dns.GetHostName returns the name of the 
                // host running the application.
                IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 2001);

                // Create a TCP/IP socket.
                Socket listener = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);

                // Bind the socket to the local endpoint and 
                // listen for incoming connections.
                try
                {
                    listener.Bind(localEndPoint);
                    listener.Listen(10);

                    // Start listening for connections.
                    while (true)
                    {
                        System.Diagnostics.Debug.WriteLine("Waiting for a connection...");
                        // Program is suspended while waiting for an incoming connection.
                        Socket handler = listener.Accept();
                        data = null;
                        message = null;
                        byte[] header = null;
                        bytes = new byte[1024];

                        // An incoming connection needs to be processed.
                        while (true)
                        {
                            int bytesRec;
                            if (header == null)
                            {
                                header = new byte[1026];
                                bytesRec = handler.Receive(header);
                            }
                                bytesRec = handler.Receive(bytes);
                            data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                            message = data.Replace('\0', ' ');
                            if (message.Split(' ').Length > 2)
                            {
                                break;
                            }
                        }

                        // Show the data on the console.
                        System.Diagnostics.Debug.WriteLine("Text received : {0}", message.Split('+').ElementAt(1));

                        if (data.Contains("DownloadCompleted"))
                        {
                            ClearResources();
                        }
                        else
                        {
                            // Echo the data back to the client.
                            StartAdaptation(message.Split('+').ElementAt(1).ToString());
                            byte[] msg = Encoding.ASCII.GetBytes(data);
                            msg.CopyTo(header, 2);

                            handler.Send(header, header.Length, 0);
                        }
                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();
                    }

                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.ToString());
                }

                System.Diagnostics.Debug.WriteLine("\nPress ENTER to continue...");
                Console.Read();

            }

            private void StartAdaptation(string command)
            {
                System.Diagnostics.Debug.WriteLine("Adapting the model");
                CreateTranscriptionFile(command);
                bool status = UpdateModel();
                if (status)
                    ZipModel();
            }

            public void CreateTranscriptionFile(string transcription)
            {
                string location = server + "recordings.transcription";
                StreamWriter file = new StreamWriter(location, false, Encoding.UTF8, 1024);
                for (int i = 0; i < 5; i++)
                    file.WriteLine("<s> " + transcription.ToUpper() + " </s> (recording-" + (i + 1).ToString() + ")");
                file.Close();
            }

            public void GetCommands()
            {
                FeatureExtractionCommand = "sphinxtrain\\bin\\Release\\sphinx_fe "
                    + "-argfile Model\\feat.params "
                    + "-samprate 8000 "
                    + "-c recordings.fileids "
                    + "-di . "
                    + "-do . "
                    + "-ei wav "
                    + "-eo mfc "
                    + "-mswav yes";

                //SendumpCommand = "sphinxtrain\\python\\cmusphinx\\sendump.py model\\sendump model\\mixture_weights";

                PocketSphinxMdefConversionCommand = "pocketsphinx\\bin\\Release\\pocketsphinx_mdef_convert "
                    + "-text Model\\mdef Model\\mdef.txt";

                CopyToNewModel = "xcopy \"" + server + "Model\" \"" + server + "NewModel\" /s /i";

                BWCommand = "sphinxtrain\\bin\\Release\\bw "
                    + "-hmmdir Model "
                    + "-moddeffn Model\\mdef.txt "
                    + "-ts2cbfn .semi. "
                    + "-feat 1s_c_d_dd "
                    + "-svspec 0-12 "
                    + "-cmn current "
                    + "-agc none "
                    + "-dictfn recordings.dic "
                    + "-ctlfn recordings.fileids "
                    + "-lsnfn recordings.transcription "
                    + "-accumdir .";

                MapAdaptationCommand = "sphinxtrain\\bin\\Release\\map_adapt "
                    + "-meanfn Model\\means "
                    + "-varfn Model\\variances "
                    + "-mixwfn Model\\mixture_weights "
                    + "-tmatfn Model\\transition_matrices "
                    + "-accumdir . "
                    + "-mapmeanfn NewModel\\means "
                    + "-mapvarfn NewModel\\variances "
                    + "-mapmixwfn NewModel\\mixture_weights "
                    + "-maptmatfn NewModel\\transition_matrices";

                MkS2SendumpCommand = "sphinxtrain\\bin\\Release\\mk_s2sendump "
                    + "-pocketsphinx yes "
                    + "-moddeffn NewModel\\mdef.txt "
                    + "-mixwfn NewModel\\mixture_weights "
                    + "-sendumpfn NewModel\\sendump";

                //Commands for testing the adapted model
                TestAdaptationCommand = "pocketsphinx\\bin\\Release\\pocketsphinx_batch "
                    + "-adcin yes "
                    + "-cepdir wav "
                    + "-cepext .wav "
                    + "-ctl recordings.fileids "
                    + "-lm recordings.dmp "
                    + "-dict recordings.dic "
                    + "-hmm NewModel "
                    + "-hyp adapation-test.hyp";

                DecodeTestCommand = "sphinxtrain\\scripts\\decode\\word_align.pl recordings.transcription adapation-test.hyp";

                DeleteFileCommand = "del \"" + server;
            }

            public void ExcecuteCommand(string command)
            {
                Process CmdProcess = new Process();
                ProcessStartInfo CmdStartInfo = new ProcessStartInfo();

                string baseFolder = AppDomain.CurrentDomain.BaseDirectory;

                CmdStartInfo.FileName = "CMD.exe ";

                CmdStartInfo.RedirectStandardError = false;
                CmdStartInfo.RedirectStandardOutput = true;
                CmdStartInfo.RedirectStandardInput = true;

                CmdStartInfo.UseShellExecute = false;
                //Dont show a command window
                CmdStartInfo.CreateNoWindow = true;

                CmdStartInfo.WorkingDirectory = baseFolder;
                CmdStartInfo.Arguments = "/C " + command;

                CmdProcess.EnableRaisingEvents = true;
                CmdProcess.StartInfo = CmdStartInfo;

                //start cmd.exe & the XCOPY process
                CmdProcess.Start();

                //set the wait period for exiting the process
                CmdProcess.WaitForExit(); //or the wait time you want         

                int ExitCode = CmdProcess.ExitCode;
                string res = CmdProcess.StandardOutput.ReadToEnd();

                //Now we need to see if the process was successful
                if (ExitCode > 0 & !CmdProcess.HasExited)
                {
                    CmdProcess.Kill();
                }

                //now clean up after ourselves
                CmdProcess.Dispose();
                CmdStartInfo = null;
            }

            public void CreateLanguageModel()
            {
                string VocabCreationCommand = "cmuclmtk\\text2wfreq < model.txt | cmuclmtk\\wfreq2vocab > model.vocab";
                string Text2IdngramCommand = "cmuclmtk\\text2idngram "
                    + "-vocab model.vocab "
                    + "-idngram model.idngram < model.txt";
                string Idngram2LmCommand = "cmuclmtk\\idngram2lm "
                    + "-vocab_type 0 "
                    + "-idngram model.idngram "
                    + "-vocab model.vocab "
                    + "-arpa model.arpa";
                string LmConvertCommand = "sphinxtrain\\bin\\Release\\sphinx_lm_convert "
                    + "-i model.arpa "
                    + "-o recordings.DMP";

                ExcecuteCommand(VocabCreationCommand);
                ExcecuteCommand(Text2IdngramCommand);
                ExcecuteCommand(Idngram2LmCommand);
                ExcecuteCommand(LmConvertCommand);
            }

            public bool UpdateModel()
            {
                ExcecuteCommand(FeatureExtractionCommand);
                ExcecuteCommand(PocketSphinxMdefConversionCommand);
                ExcecuteCommand(CopyToNewModel);
                ExcecuteCommand(BWCommand);
                ExcecuteCommand(MapAdaptationCommand);
                ExcecuteCommand(MkS2SendumpCommand);
                //ExcecuteCommand(TestAdaptationCommand);
                //ExcecuteCommand(DecodeTestCommand);
                return true;
            }

            public void ClearResources()
            {
                ExcecuteCommand(DeleteFileCommand + "recording-1.mfc\" /Q");
                ExcecuteCommand(DeleteFileCommand + "recording-2.mfc\" /Q");
                ExcecuteCommand(DeleteFileCommand + "recording-3.mfc\" /Q");
                ExcecuteCommand(DeleteFileCommand + "recording-4.mfc\" /Q");
                ExcecuteCommand(DeleteFileCommand + "recording-5.mfc\" /Q");

                ExcecuteCommand(DeleteFileCommand + "recording-1.wav\" /Q");
                ExcecuteCommand(DeleteFileCommand + "recording-2.wav\" /Q");
                ExcecuteCommand(DeleteFileCommand + "recording-3.wav\" /Q");
                ExcecuteCommand(DeleteFileCommand + "recording-4.wav\" /Q");
                ExcecuteCommand(DeleteFileCommand + "recording-5.wav\" /Q");

                ExcecuteCommand(DeleteFileCommand + "NewModel\\*\" /Q");
            }

            protected void ZipModel()
            {
                string[] filesPath = Directory.GetFiles(server + @"NewModel\");
                ZipFile zip = new ZipFile();
                foreach (string path in filesPath)
                {
                    zip.AddFile(path, "");
                }

                string zipFilePath = server + @"NewModel\new-model.zip";

                zip.Save(zipFilePath);
            }
        }

        protected void StartServer_Click(object sender, EventArgs e)
        {
            SynchronousSocketListener ssl = new SynchronousSocketListener();
            ssl.StartListening();
        }
    }
}